import 'package:dio/dio.dart';
import 'package:servit_app/features/provider_profile/domain/provider_profile.dart';
import 'package:servit_app/features/provider_profile/domain/public_provider_profile.dart';

class ProviderRepository {
  ProviderRepository(this._dio);

  final Dio _dio;

  Future<ProviderProfile> getMe() async {
    final response = await _dio.get('/providers/me');
    return ProviderProfile.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ProviderProfile> updateCategories(List<int> categoryIds) async {
    final response = await _dio.put(
      '/providers/me/categories',
      data: {'categoryIds': categoryIds},
    );
    return ProviderProfile.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ProviderProfile> updateLocation({required double lat, required double lng}) async {
    final response = await _dio.put(
      '/providers/me/location',
      data: {'lat': lat, 'lng': lng},
    );
    return ProviderProfile.fromJson(response.data as Map<String, dynamic>);
  }

  Future<PublicProviderProfile> getPublicProfile(String providerId) async {
    final response = await _dio.get('/providers/$providerId');
    return PublicProviderProfile.fromJson(response.data as Map<String, dynamic>);
  }
}
