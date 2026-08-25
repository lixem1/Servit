import 'package:dio/dio.dart';

String describeApiError(Object error) {
  if (error is DioException) {
    final data = error.response?.data;

    // ASP.NET Core's automatic [ApiController] model validation (400).
    if (data is Map && data['errors'] is Map) {
      final errors = (data['errors'] as Map).values.expand((v) => v as List);
      if (errors.isNotEmpty) return errors.join('\n');
    }

    // Our controllers' BadRequest(IEnumerable<string>) responses.
    if (data is List) {
      return data.join('\n');
    }

    // Plain-string BadRequest/Unauthorized responses.
    if (data is String && data.isNotEmpty) {
      return data;
    }

    if (error.response?.statusCode == 401) {
      return 'Credenciales inválidas.';
    }
    return 'No se pudo conectar con el servidor. Verifica tu conexión.';
  }
  return 'Ocurrió un error inesperado.';
}
