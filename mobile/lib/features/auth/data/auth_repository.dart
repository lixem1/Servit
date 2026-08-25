import 'package:dio/dio.dart';
import 'package:servit_app/features/auth/domain/auth_session.dart';
import 'package:servit_app/features/auth/domain/google_auth_result.dart';

class AuthRepository {
  AuthRepository(this._dio);

  final Dio _dio;

  Future<AuthSession> register({
    required String fullName,
    required String email,
    required String password,
    required String role,
  }) async {
    final response = await _dio.post('/auth/register', data: {
      'fullName': fullName,
      'email': email,
      'password': password,
      'role': role,
    });
    return AuthSession.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final response = await _dio.post('/auth/login', data: {
      'email': email,
      'password': password,
    });
    return AuthSession.fromJson(response.data as Map<String, dynamic>);
  }

  Future<GoogleAuthResult> google({required String idToken, String? role}) async {
    final response = await _dio.post('/auth/google', data: {
      'idToken': idToken,
      'role': ?role,
    });
    return GoogleAuthResult.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> forgotPassword({required String email}) async {
    await _dio.post('/auth/forgot-password', data: {'email': email});
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) async {
    await _dio.post('/auth/reset-password', data: {
      'email': email,
      'code': code,
      'newPassword': newPassword,
    });
  }
}
