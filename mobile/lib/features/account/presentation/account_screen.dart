import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/account/presentation/account_controller.dart';

class AccountScreen extends ConsumerStatefulWidget {
  const AccountScreen({super.key});

  @override
  ConsumerState<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends ConsumerState<AccountScreen> {
  final _nameFormKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();

  final _passwordFormKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  bool _obscureCurrentPassword = true;
  bool _obscureNewPassword = true;

  bool _nameSynced = false;

  @override
  void dispose() {
    _nameController.dispose();
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    super.dispose();
  }

  Future<void> _saveName() async {
    if (!_nameFormKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    await ref.read(accountControllerProvider.notifier).updateFullName(_nameController.text.trim());
  }

  Future<void> _savePassword(bool hasPassword) async {
    if (!_passwordFormKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    await ref.read(accountControllerProvider.notifier).changePassword(
          currentPassword: hasPassword ? _currentPasswordController.text : null,
          newPassword: _newPasswordController.text,
        );
    _currentPasswordController.clear();
    _newPasswordController.clear();
  }

  Future<void> _confirmDeleteAccount() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Eliminar cuenta'),
        content: const Text(
          'Esta acción es irreversible. Se cerrará tu sesión y no podrás volver a '
          'iniciar sesión con esta cuenta. Tu historial de servicios y reseñas se conservará '
          'para la otra parte involucrada.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Theme.of(context).colorScheme.error),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Eliminar'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    await ref.read(accountControllerProvider.notifier).deleteAccount();
  }

  @override
  Widget build(BuildContext context) {
    final accountState = ref.watch(accountControllerProvider);
    final isLoading = accountState.isLoading;

    ref.listen(accountControllerProvider, (previous, next) {
      final error = next.error;
      if (error != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(describeApiError(error))),
        );
      }
    });

    final profile = accountState.valueOrNull;
    if (profile != null && !_nameSynced) {
      _nameController.text = profile.fullName;
      _nameSynced = true;
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Mi cuenta')),
      body: profile == null
          ? const Center(child: CircularProgressIndicator())
          : SafeArea(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Perfil', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 4),
                    Text(profile.email, style: Theme.of(context).textTheme.bodyMedium),
                    const SizedBox(height: 16),
                    Form(
                      key: _nameFormKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          TextFormField(
                            controller: _nameController,
                            decoration: const InputDecoration(labelText: 'Nombre completo'),
                            validator: (value) =>
                                (value == null || value.trim().isEmpty) ? 'Ingresa tu nombre' : null,
                          ),
                          const SizedBox(height: 12),
                          Align(
                            alignment: Alignment.centerRight,
                            child: FilledButton(
                              onPressed: isLoading ? null : _saveName,
                              child: const Text('Guardar'),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 32),
                    Text(
                      profile.hasPassword ? 'Cambiar contraseña' : 'Establecer contraseña',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: 16),
                    Form(
                      key: _passwordFormKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (profile.hasPassword) ...[
                            TextFormField(
                              controller: _currentPasswordController,
                              obscureText: _obscureCurrentPassword,
                              decoration: InputDecoration(
                                labelText: 'Contraseña actual',
                                suffixIcon: IconButton(
                                  icon: Icon(_obscureCurrentPassword
                                      ? Icons.visibility_outlined
                                      : Icons.visibility_off_outlined),
                                  onPressed: () =>
                                      setState(() => _obscureCurrentPassword = !_obscureCurrentPassword),
                                ),
                              ),
                              validator: (value) => (value == null || value.isEmpty)
                                  ? 'Ingresa tu contraseña actual'
                                  : null,
                            ),
                            const SizedBox(height: 12),
                          ],
                          TextFormField(
                            controller: _newPasswordController,
                            obscureText: _obscureNewPassword,
                            decoration: InputDecoration(
                              labelText: 'Nueva contraseña',
                              suffixIcon: IconButton(
                                icon: Icon(_obscureNewPassword
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined),
                                onPressed: () => setState(() => _obscureNewPassword = !_obscureNewPassword),
                              ),
                            ),
                            validator: (value) => (value == null || value.length < 8)
                                ? 'Mínimo 8 caracteres'
                                : null,
                          ),
                          const SizedBox(height: 12),
                          Align(
                            alignment: Alignment.centerRight,
                            child: FilledButton(
                              onPressed: isLoading ? null : () => _savePassword(profile.hasPassword),
                              child: Text(profile.hasPassword ? 'Cambiar contraseña' : 'Establecer contraseña'),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 40),
                    Text('Zona de riesgo', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 12),
                    OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: Theme.of(context).colorScheme.error,
                        side: BorderSide(color: Theme.of(context).colorScheme.error),
                      ),
                      onPressed: isLoading ? null : _confirmDeleteAccount,
                      child: const Text('Eliminar cuenta'),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
