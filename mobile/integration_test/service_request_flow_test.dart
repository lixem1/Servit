import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:servit_app/main.dart';

// Matching provider account, driven directly over HTTP from within this test
// (in parallel with the on-device UI flow) so the whole scenario is
// self-contained and deterministic — no external orchestration required.
final _providerFullName = 'E2E Provider ${DateTime.now().microsecondsSinceEpoch}';
const _providerPassword = 'Password123!';

class _ProviderActor {
  _ProviderActor()
      : _dio = Dio(BaseOptions(baseUrl: 'http://localhost:5220/api')),
        _email = 'e2e_provider_${DateTime.now().microsecondsSinceEpoch}@servit.test';

  final Dio _dio;
  final String _email;
  String? _token;

  Options get _auth => Options(headers: {'Authorization': 'Bearer $_token'});

  Future<void> setUp() async {
    await _dio.post('/auth/register', data: {
      'fullName': _providerFullName,
      'email': _email,
      'password': _providerPassword,
      'role': 'Provider',
    });

    final login = await _dio.post('/auth/login', data: {
      'email': _email,
      'password': _providerPassword,
    });
    _token = login.data['token'] as String;

    final categories = await _dio.get('/categories', options: _auth);
    final categoryId = (categories.data as List<dynamic>)
        .cast<Map<String, dynamic>>()
        .firstWhere((c) => c['name'] == 'Electricidad')['id'];

    await _dio.put('/providers/me/categories',
        data: {
          'categoryIds': [categoryId]
        },
        options: _auth);
    await _dio.put('/providers/me/location',
        data: {'lat': -33.4489, 'lng': -70.6693}, options: _auth);
  }

  Future<String> findRequestId({
    required String descriptionContains,
    required DateTime createdAfter,
  }) async {
    for (var i = 0; i < 30; i++) {
      final nearby = await _dio.get('/service-requests/nearby', options: _auth);
      for (final r in (nearby.data as List<dynamic>).cast<Map<String, dynamic>>()) {
        if (r['categoryName'] == 'Electricidad' &&
            (r['description'] as String).contains(descriptionContains) &&
            r['status'] == 'Pending' &&
            DateTime.parse(r['createdAt'] as String).isAfter(createdAfter)) {
          return r['id'] as String;
        }
      }
      await Future<void>.delayed(const Duration(seconds: 1));
    }
    throw StateError('Provider never saw the request appear in /nearby');
  }

  Future<String> respond({
    required String requestId,
    required String message,
    required double proposedPrice,
  }) async {
    final response = await _dio.post(
      '/service-requests/$requestId/responses',
      data: {'message': message, 'proposedPrice': proposedPrice},
      options: _auth,
    );
    return response.data['id'] as String;
  }
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('customer creates request, rejects one offer, accepts the next, reviews the provider',
      (tester) async {
    final testStart = DateTime.now().toUtc();
    final uniqueEmail = 'e2e_customer_${DateTime.now().microsecondsSinceEpoch}@servit.test';
    const password = 'Password123!';
    const fullName = 'Cliente E2E';

    final providerActor = _ProviderActor();
    await providerActor.setUp();

    await tester.pumpWidget(const ProviderScope(child: ServitApp()));
    await tester.pumpAndSettle();

    // A prior test run may have left a session persisted in secure storage.
    if (find.byIcon(Icons.logout).evaluate().isNotEmpty) {
      await tester.tap(find.byIcon(Icons.logout));
      await tester.pumpAndSettle();
    }

    expect(find.text('Bienvenido'), findsWidgets);

    await tester.tap(find.text('¿No tienes cuenta? Regístrate'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Nombre completo'), fullName);
    await tester.enterText(find.widgetWithText(TextFormField, 'Correo electrónico'), uniqueEmail);
    await tester.enterText(find.widgetWithText(TextFormField, 'Contraseña'), password);
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.text('Registrarme'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Registrarme'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.textContaining('¡Hola, $fullName!'), findsOneWidget);

    await tester.tap(find.text('Crear solicitud'));
    await tester.pumpAndSettle();

    expect(find.text('¿Qué necesitas?'), findsOneWidget);

    await tester.tap(find.byType(DropdownButtonFormField<int>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Electricidad').last);
    await tester.pumpAndSettle();

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Descripción'),
      'Se cayó un enchufe y necesito revisión urgente.',
    );
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.text('Usar mi ubicación actual'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Usar mi ubicación actual'));
    await tester.pumpAndSettle(const Duration(seconds: 3));

    expect(find.textContaining('Ubicación lista'), findsOneWidget);

    await tester.ensureVisible(find.text('Publicar solicitud'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Publicar solicitud'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    // Submitting pops back to Home.
    expect(find.textContaining('¡Hola, $fullName!'), findsOneWidget);

    await tester.tap(find.text('Mis solicitudes'));
    await tester.pumpAndSettle();

    expect(find.text('Electricidad'), findsOneWidget);
    await tester.tap(find.text('Electricidad'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.textContaining('Todavía no hay respuestas'), findsOneWidget);

    final requestId = await providerActor.findRequestId(
      descriptionContains: 'enchufe',
      createdAfter: testStart,
    );
    await providerActor.respond(
      requestId: requestId,
      message: 'Puedo ir hoy mismo',
      proposedPrice: 15000,
    );

    // Wait for the first offer to arrive via the real SignalR push.
    var sawResponse = false;
    for (var i = 0; i < 20; i++) {
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle(const Duration(milliseconds: 500));
      if (find.textContaining(_providerFullName).evaluate().isNotEmpty) {
        sawResponse = true;
        break;
      }
    }
    expect(sawResponse, isTrue, reason: 'Provider response never arrived via SignalR push');

    // Tap the provider's name to view their public profile before deciding.
    await tester.tap(find.text(_providerFullName).first);
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Perfil del proveedor'), findsOneWidget);
    expect(find.text('0.0'), findsOneWidget);
    expect(find.textContaining('todavía no tiene reseñas'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();

    // Reject the first offer — the request should stay open for other offers.
    expect(find.text('Rechazar'), findsOneWidget);
    await tester.tap(find.text('Rechazar'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Rechazada'), findsOneWidget);
    expect(find.text('Aceptar'), findsNothing);

    await providerActor.respond(
      requestId: requestId,
      message: 'Puedo pasar mañana a primera hora',
      proposedPrice: 18000,
    );

    var sawSecondResponse = false;
    for (var i = 0; i < 20; i++) {
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle(const Duration(milliseconds: 500));
      if (find.text('Aceptar').evaluate().isNotEmpty) {
        sawSecondResponse = true;
        break;
      }
    }
    expect(sawSecondResponse, isTrue,
        reason: 'Second provider response never arrived via SignalR push');

    await tester.ensureVisible(find.text('Aceptar'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Aceptar'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Aceptada'), findsOneWidget);
    expect(find.text('Aceptar'), findsNothing);

    await tester.ensureVisible(find.text('Marcar como completado'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Marcar como completado'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Califica al proveedor'), findsOneWidget);

    await tester.ensureVisible(find.text('Enviar reseña'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Enviar reseña'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Califica al proveedor'), findsNothing);

    await tester.tap(find.text(_providerFullName).first);
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.text('Perfil del proveedor'), findsOneWidget);
    expect(find.text('5.0'), findsOneWidget);
    expect(find.textContaining('todavía no tiene reseñas'), findsNothing);
  });
}
