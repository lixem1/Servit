import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

typedef TokenProvider = Future<String?> Function();
typedef OnUnauthorized = void Function();

class ApiClient {
  ApiClient({
    required TokenProvider tokenProvider,
    this.onUnauthorized,
  }) : dio = Dio(BaseOptions(baseUrl: _resolveBaseUrl())) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await tokenProvider();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) {
          if (error.response?.statusCode == 401) {
            onUnauthorized?.call();
          }
          handler.next(error);
        },
      ),
    );
  }

  final Dio dio;
  final OnUnauthorized? onUnauthorized;

  String get hubBaseUrl => dio.options.baseUrl.replaceFirst(RegExp(r'/api$'), '');

  static String _resolveBaseUrl() {
    const overrideHost = String.fromEnvironment('API_HOST');
    if (overrideHost.isNotEmpty) {
      return 'http://$overrideHost:5220/api';
    }

    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5220/api';
    }
    return 'http://localhost:5220/api';
  }
}
