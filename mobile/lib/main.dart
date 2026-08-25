import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/router/app_router.dart';
import 'package:servit_app/core/theme/app_theme.dart';

void main() {
  runApp(const ProviderScope(child: ServitApp()));
}

class ServitApp extends ConsumerWidget {
  const ServitApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: 'Servit',
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: ThemeMode.system,
      routerConfig: router,
    );
  }
}
