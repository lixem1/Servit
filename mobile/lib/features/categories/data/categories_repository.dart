import 'package:dio/dio.dart';
import 'package:servit_app/features/categories/domain/category.dart';

class CategoriesRepository {
  CategoriesRepository(this._dio);

  final Dio _dio;

  Future<List<Category>> getAll() async {
    final response = await _dio.get('/categories');
    return (response.data as List)
        .map((json) => Category.fromJson(json as Map<String, dynamic>))
        .toList();
  }
}
