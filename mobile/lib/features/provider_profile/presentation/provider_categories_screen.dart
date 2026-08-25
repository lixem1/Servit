import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/categories/presentation/categories_controller.dart';
import 'package:servit_app/features/provider_profile/presentation/provider_profile_controller.dart';

class ProviderCategoriesScreen extends ConsumerStatefulWidget {
  const ProviderCategoriesScreen({super.key});

  @override
  ConsumerState<ProviderCategoriesScreen> createState() => _ProviderCategoriesScreenState();
}

class _ProviderCategoriesScreenState extends ConsumerState<ProviderCategoriesScreen> {
  Set<int>? _selected;

  @override
  Widget build(BuildContext context) {
    final categoriesAsync = ref.watch(categoriesControllerProvider);
    final profileAsync = ref.watch(providerProfileControllerProvider);

    ref.listen(providerProfileControllerProvider, (previous, next) {
      final error = next.error;
      if (error != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(describeApiError(error))),
        );
      }
      final profile = next.valueOrNull;
      if (profile != null) {
        _selected ??= profile.categoryIds.toSet();
      }
    });

    final isSaving = profileAsync.isLoading;

    return Scaffold(
      appBar: AppBar(title: const Text('Mis categorías')),
      body: SafeArea(
        child: categoriesAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => Center(child: Text(describeApiError(error))),
          data: (categories) {
            _selected ??= profileAsync.valueOrNull?.categoryIds.toSet() ?? {};
            final selected = _selected!;

            return Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('¿Qué servicios ofreces?', style: Theme.of(context).textTheme.headlineSmall),
                  const SizedBox(height: 4),
                  Text(
                    'Selecciona una o más categorías. Los clientes de esas categorías podrán encontrarte.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: categories.map((category) {
                      final isSelected = selected.contains(category.id);
                      return FilterChip(
                        label: Text(category.name),
                        selected: isSelected,
                        onSelected: (value) {
                          setState(() {
                            if (value) {
                              selected.add(category.id);
                            } else {
                              selected.remove(category.id);
                            }
                          });
                        },
                      );
                    }).toList(),
                  ),
                  const Spacer(),
                  FilledButton(
                    onPressed: isSaving
                        ? null
                        : () async {
                            await ref
                                .read(providerProfileControllerProvider.notifier)
                                .updateCategories(selected.toList());
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(content: Text('Categorías guardadas')),
                              );
                            }
                          },
                    child: isSaving
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : const Text('Guardar'),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}
