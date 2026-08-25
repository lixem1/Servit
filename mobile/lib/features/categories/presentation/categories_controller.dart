import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';
import 'package:servit_app/features/categories/data/categories_repository.dart';
import 'package:servit_app/features/categories/domain/category.dart';

final categoriesRepositoryProvider = Provider((ref) {
  return CategoriesRepository(ref.read(apiClientProvider).dio);
});

final categoriesControllerProvider = AsyncNotifierProvider<CategoriesController, List<Category>>(
  CategoriesController.new,
);

class CategoriesController extends AsyncNotifier<List<Category>> {
  @override
  Future<List<Category>> build() {
    return ref.read(categoriesRepositoryProvider).getAll();
  }
}
