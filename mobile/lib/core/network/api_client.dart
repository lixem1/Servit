import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

typedef TokenProvider = Future<String?> Function();

class ApiClient {
  ApiClient({required TokenProvider tokenProvider})
      : dio = Dio(BaseOptions(baseUrl: _resolveBaseUrl())) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await tokenProvider();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
      ),
    );
  }

  final Dio dio;

  String get hubBaseUrl => dio.options.baseUrl.replaceFirst(RegExp(r'/api$'), '');

  // Override with --dart-define=API_HOST=<lan-ip> when running on a physical
  // device, since it can't reach the dev machine via localhost/10.0.2.2.
  static String _resolveBaseUrl() {
    const overrideHost = String.fromEnvironment('API_HOST');
    if (overrideHost.isNotEmpty) {
      return 'http://$overrideHost:5220/api';
    }

    // Android emulator can't reach the host via localhost; it maps to 10.0.2.2.
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5220/api';
    }
    return 'http://localhost:5220/api';
  }
}
