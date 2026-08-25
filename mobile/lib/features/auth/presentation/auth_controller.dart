import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:servit_app/core/network/api_client.dart';
import 'package:servit_app/features/auth/data/auth_repository.dart';
import 'package:servit_app/features/auth/domain/auth_session.dart';
import 'package:servit_app/features/auth/domain/google_auth_result.dart';

const _sessionStorageKey = 'auth_session';

final secureStorageProvider = Provider((ref) => const FlutterSecureStorage());

final authTokenProviderProvider = Provider<TokenProvider>((ref) {
  final storage = ref.read(secureStorageProvider);
  return () async {
    final raw = await storage.read(key: _sessionStorageKey);
    if (raw == null) return null;
    return AuthSession.fromJson(jsonDecode(raw) as Map<String, dynamic>).token;
  };
});

final apiClientProvider = Provider((ref) {
  return ApiClient(tokenProvider: ref.read(authTokenProviderProvider));
});

final authRepositoryProvider = Provider((ref) {
  return AuthRepository(ref.read(apiClientProvider).dio);
});

final authControllerProvider =
    AsyncNotifierProvider<AuthController, AuthSession?>(AuthController.new);

class AuthController extends AsyncNotifier<AuthSession?> {
  @override
  Future<AuthSession?> build() async {
    final raw = await ref.read(secureStorageProvider).read(key: _sessionStorageKey);
    if (raw == null) return null;
    return AuthSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> register({
    required String fullName,
    required String email,
    required String password,
    required String role,
  }) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final session = await ref.read(authRepositoryProvider).register(
            fullName: fullName,
            email: email,
            password: password,
            role: role,
          );
      await _persist(session);
      return session;
    });
  }

  Future<void> login({required String email, required String password}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final session = await ref.read(authRepositoryProvider).login(
            email: email,
            password: password,
          );
      await _persist(session);
      return session;
    });
  }

  String? _pendingGoogleIdToken;
  bool _googleSignInInitialized = false;

  Future<GoogleAuthResult?> signInWithGoogle() async {
    state = const AsyncLoading();
    GoogleAuthResult? result;
    final newState = await AsyncValue.guard(() async {
      if (!_googleSignInInitialized) {
        await _googleSignIn.initialize();
        _googleSignInInitialized = true;
      }
      final account = await _googleSignIn.authenticate();
      final idToken = account.authentication.idToken;
      if (idToken == null) {
        throw StateError('Google no devolvió un idToken.');
      }
      result = await ref.read(authRepositoryProvider).google(idToken: idToken);
      if (result!.requiresRole) {
        _pendingGoogleIdToken = idToken;
        return state.valueOrNull;
      }
      final session = result!.session!;
      await _persist(session);
      return session;
    });
    state = newState;
    return result;
  }

  Future<void> completeGoogleSignIn({required String role}) async {
    final idToken = _pendingGoogleIdToken;
    if (idToken == null) return;
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final result = await ref.read(authRepositoryProvider).google(idToken: idToken, role: role);
      final session = result.session!;
      _pendingGoogleIdToken = null;
      await _persist(session);
      return session;
    });
  }

  Future<void> logout() async {
    await ref.read(secureStorageProvider).delete(key: _sessionStorageKey);
    state = const AsyncData(null);
  }

  final _googleSignIn = GoogleSignIn.instance;

  Future<void> _persist(AuthSession session) async {
    await ref
        .read(secureStorageProvider)
        .write(key: _sessionStorageKey, value: jsonEncode(session.toJson()));
  }
}
