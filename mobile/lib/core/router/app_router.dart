import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:servit_app/features/account/presentation/account_screen.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';
import 'package:servit_app/features/auth/presentation/forgot_password_screen.dart';
import 'package:servit_app/features/auth/presentation/login_screen.dart';
import 'package:servit_app/features/auth/presentation/register_screen.dart';
import 'package:servit_app/features/home/presentation/home_screen.dart';
import 'package:servit_app/features/provider_profile/presentation/provider_categories_screen.dart';
import 'package:servit_app/features/provider_profile/presentation/provider_profile_view_screen.dart';
import 'package:servit_app/features/service_requests/presentation/customer/create_request_screen.dart';
import 'package:servit_app/features/service_requests/presentation/customer/my_requests_screen.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';
import 'package:servit_app/features/service_requests/presentation/customer/request_detail_screen.dart';
import 'package:servit_app/features/service_requests/presentation/provider/nearby_requests_screen.dart';
import 'package:servit_app/features/service_requests/presentation/provider/my_services_screen.dart';

class _AuthRefreshNotifier extends ChangeNotifier {
  _AuthRefreshNotifier(Ref ref) {
    ref.listen(authControllerProvider, (previous, next) {
      if (previous?.isLoading != next.isLoading || previous?.valueOrNull != next.valueOrNull) {
        notifyListeners();
      }
    });
  }
}

final routerProvider = Provider<GoRouter>((ref) {
  final refreshNotifier = _AuthRefreshNotifier(ref);

  return GoRouter(
    initialLocation: '/login',
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      if (authState.isLoading) return null;

      final isLoggedIn = authState.valueOrNull != null;
      final isAuthRoute = state.matchedLocation == '/login' ||
          state.matchedLocation == '/register' ||
          state.matchedLocation == '/forgot-password';

      if (!isLoggedIn && !isAuthRoute) return '/login';
      if (isLoggedIn && isAuthRoute) return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(path: '/register', builder: (context, state) => const RegisterScreen()),
      GoRoute(path: '/forgot-password', builder: (context, state) => const ForgotPasswordScreen()),
      GoRoute(path: '/home', builder: (context, state) => const HomeScreen()),
      GoRoute(path: '/account', builder: (context, state) => const AccountScreen()),
      GoRoute(path: '/requests/new', builder: (context, state) => const CreateRequestScreen()),
      GoRoute(path: '/requests/mine', builder: (context, state) => const MyRequestsScreen()),
      GoRoute(
        path: '/requests/:id',
        builder: (context, state) => RequestDetailScreen(
          requestId: state.pathParameters['id']!,
          initialRequest: state.extra as ServiceRequest?,
        ),
      ),
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) =>
            ProviderProfileViewScreen(providerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/provider/categories',
        builder: (context, state) => const ProviderCategoriesScreen(),
      ),
      GoRoute(
        path: '/provider/nearby',
        builder: (context, state) => const NearbyRequestsScreen(),
      ),
      GoRoute(
        path: '/provider/services',
        builder: (context, state) => const MyServicesScreen(),
      ),
    ],
  );
});
