import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/location/location_service.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';
import 'package:servit_app/features/provider_profile/data/provider_repository.dart';
import 'package:servit_app/features/provider_profile/domain/provider_profile.dart';
import 'package:servit_app/features/provider_profile/domain/public_provider_profile.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';

final locationServiceProvider = Provider((ref) => LocationService());

final providerRepositoryProvider = Provider((ref) {
  return ProviderRepository(ref.read(apiClientProvider).dio);
});

final providerProfileControllerProvider =
    AsyncNotifierProvider<ProviderProfileController, ProviderProfile?>(ProviderProfileController.new);

class ProviderProfileController extends AsyncNotifier<ProviderProfile?> {
  @override
  Future<ProviderProfile?> build() async {
    return ref.read(providerRepositoryProvider).getMe();
  }

  Future<void> updateCategories(List<int> categoryIds) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(providerRepositoryProvider).updateCategories(categoryIds),
    );
    // Previously-created pending requests matching the newly-added categories
    // are only ever pushed once, at creation time — refetch so they surface now.
    ref.invalidate(nearbyRequestsControllerProvider);
  }

  Future<void> refreshLocation() async {
    final position = await ref.read(locationServiceProvider).getCurrentPosition();
    state = await AsyncValue.guard(
      () => ref.read(providerRepositoryProvider).updateLocation(
            lat: position.latitude,
            lng: position.longitude,
          ),
    );
  }
}

final publicProviderProfileProvider =
    FutureProvider.autoDispose.family<PublicProviderProfile, String>((ref, providerId) {
  return ref.read(providerRepositoryProvider).getPublicProfile(providerId);
});
