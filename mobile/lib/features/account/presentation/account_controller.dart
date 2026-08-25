import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/features/account/data/account_repository.dart';
import 'package:servit_app/features/account/domain/account_profile.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';

final accountRepositoryProvider = Provider((ref) {
  return AccountRepository(ref.read(apiClientProvider).dio);
});

final accountControllerProvider =
    AsyncNotifierProvider<AccountController, AccountProfile>(AccountController.new);

class AccountController extends AsyncNotifier<AccountProfile> {
  @override
  Future<AccountProfile> build() async {
    return ref.read(accountRepositoryProvider).getMe();
  }

  Future<void> updateFullName(String fullName) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(accountRepositoryProvider).updateProfile(fullName: fullName),
    );
  }

  Future<void> changePassword({String? currentPassword, required String newPassword}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(accountRepositoryProvider).changePassword(
            currentPassword: currentPassword,
            newPassword: newPassword,
          );
      return ref.read(accountRepositoryProvider).getMe();
    });
  }

  Future<void> deleteAccount() async {
    await ref.read(accountRepositoryProvider).deleteAccount();
    await ref.read(authControllerProvider.notifier).logout();
  }
}
