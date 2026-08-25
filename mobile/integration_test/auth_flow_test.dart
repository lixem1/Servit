import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:servit_app/main.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('register -> home -> logout -> login', (tester) async {
    final uniqueEmail = 'e2e_${DateTime.now().microsecondsSinceEpoch}@servit.test';
    const password = 'Password123!';
    const fullName = 'Usuario E2E';

    await tester.pumpWidget(const ProviderScope(child: ServitApp()));
    await tester.pumpAndSettle();

    expect(find.text('Bienvenido'), findsWidgets);

    await tester.tap(find.text('¿No tienes cuenta? Regístrate'));
    await tester.pumpAndSettle();

    expect(find.text('Crear cuenta'), findsWidgets);

    await tester.enterText(find.widgetWithText(TextFormField, 'Nombre completo'), fullName);
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Correo electrónico'), uniqueEmail);
    await tester.enterText(find.widgetWithText(TextFormField, 'Contraseña'), password);
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.text('Registrarme'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Registrarme'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    final snackBarFinder = find.byType(SnackBar);
    if (snackBarFinder.evaluate().isNotEmpty) {
      final texts = tester
          .widgetList<Text>(find.descendant(of: snackBarFinder, matching: find.byType(Text)))
          .map((t) => t.data)
          .join(' | ');
      fail('SnackBar shown after register: $texts');
    }

    expect(find.textContaining('¡Hola, $fullName!'), findsOneWidget);

    await tester.tap(find.byIcon(Icons.logout));
    await tester.pumpAndSettle();

    expect(find.text('Bienvenido'), findsWidgets);

    await tester.enterText(
        find.widgetWithText(TextFormField, 'Correo electrónico'), uniqueEmail);
    await tester.enterText(find.widgetWithText(TextFormField, 'Contraseña'), password);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Ingresar'));
    await tester.pumpAndSettle(const Duration(seconds: 2));

    expect(find.textContaining('¡Hola, $fullName!'), findsOneWidget);
  });
}
