import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:servit_app/features/auth/domain/roles.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).valueOrNull;
    final isProvider = session?.role == Roles.provider;
    final roleLabel = isProvider ? 'Prestador de servicios' : 'Cliente';
    final initial = (session?.fullName.isNotEmpty ?? false) ? session!.fullName[0].toUpperCase() : '?';

    // Both roles need a live connection to receive their respective events —
    // not just providers.
    ref.read(realtimeServiceProvider).connect();

    ref.listen(responseAcceptedEventsProvider, (previous, next) {
      final response = next.valueOrNull;
      if (response == null) return;
      ref.invalidate(myServicesControllerProvider);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('¡Tu oferta fue aceptada! Revisa Mis servicios.')),
      );
    });

    ref.listen(responseDeclinedEventsProvider, (previous, next) {
      final response = next.valueOrNull;
      if (response == null) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Tu cotización fue rechazada. Puedes enviar una nueva oferta.'),
        ),
      );
    });

    ref.listen(serviceRequestCancelledEventsProvider, (previous, next) {
      final info = next.valueOrNull;
      if (info == null) return;
      ref.invalidate(nearbyRequestsControllerProvider);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('El cliente canceló la solicitud de ${info.categoryName}.'),
        ),
      );
    });

    if (isProvider) {
      ref.listen(requestCreatedEventsProvider, (previous, next) {
        final request = next.valueOrNull;
        if (request == null) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Nueva solicitud cercana: ${request.categoryName}')),
        );
      });
    } else {
      ref.listen(responseReceivedEventsProvider, (previous, next) {
        final response = next.valueOrNull;
        if (response == null) return;
        ref.invalidate(myRequestsControllerProvider);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Recibiste una nueva cotización.')),
        );
      });
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Servit'),
        actions: [
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.push('/account'),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              const SizedBox(height: 24),
              CircleAvatar(
                radius: 40,
                backgroundColor: Theme.of(context).colorScheme.primary.withValues(alpha: 0.12),
                child: Text(
                  initial,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                        color: Theme.of(context).colorScheme.primary,
                      ),
                ),
              ),
              const SizedBox(height: 20),
              Text(
                '¡Hola, ${session?.fullName ?? ''}!',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 6),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  roleLabel,
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.primary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
              ),
              const SizedBox(height: 40),
              if (isProvider) ...[
                _HomeActionCard(
                  icon: Icons.near_me_outlined,
                  title: 'Solicitudes cercanas',
                  subtitle: 'Revisa y responde solicitudes de clientes cerca de ti.',
                  onTap: () => context.push('/provider/nearby'),
                ),
                const SizedBox(height: 12),
                _HomeActionCard(
                  icon: Icons.category_outlined,
                  title: 'Mis categorías',
                  subtitle: 'Elige qué servicios ofreces para que los clientes te encuentren.',
                  onTap: () => context.push('/provider/categories'),
                ),
                const SizedBox(height: 12),
                _HomeActionCard(
                  icon: Icons.handyman_outlined,
                  title: 'Mis servicios',
                  subtitle: 'Revisa tus trabajos pendientes y los que ya realizaste.',
                  onTap: () => context.push('/provider/services'),
                ),
              ] else ...[
                _HomeActionCard(
                  icon: Icons.add_circle_outline,
                  title: 'Crear solicitud',
                  subtitle: 'Publica lo que necesitas y recibe respuestas de proveedores cercanos.',
                  onTap: () => context.push('/requests/new'),
                ),
                const SizedBox(height: 12),
                _HomeActionCard(
                  icon: Icons.list_alt_outlined,
                  title: 'Mis solicitudes',
                  subtitle: 'Revisa el estado de tus solicitudes y sus respuestas.',
                  onTap: () => context.push('/requests/mine'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _HomeActionCard extends StatelessWidget {
  const _HomeActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Row(
            children: [
              Icon(icon, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 2),
                    Text(subtitle, style: Theme.of(context).textTheme.bodyMedium),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
