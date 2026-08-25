import 'package:dio/dio.dart';
import 'package:servit_app/features/account/domain/account_profile.dart';

class AccountRepository {
  AccountRepository(this._dio);

  final Dio _dio;

  Future<AccountProfile> getMe() async {
    final response = await _dio.get('/account/me');
    return AccountProfile.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AccountProfile> updateProfile({required String fullName}) async {
    final response = await _dio.put('/account/me', data: {'fullName': fullName});
    return AccountProfile.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> changePassword({String? currentPassword, required String newPassword}) async {
    await _dio.post('/account/change-password', data: {
      'currentPassword': ?currentPassword,
      'newPassword': newPassword,
    });
  }

  Future<void> deleteAccount() async {
    await _dio.delete('/account/me');
  }
}
