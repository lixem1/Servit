import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';
import 'package:servit_app/features/service_requests/presentation/status_labels.dart';
import 'package:servit_app/features/service_requests/presentation/widgets/attachments_viewer.dart';

class MyServicesScreen extends ConsumerStatefulWidget {
  const MyServicesScreen({super.key});

  @override
  ConsumerState<MyServicesScreen> createState() => _MyServicesScreenState();
}

class _MyServicesScreenState extends ConsumerState<MyServicesScreen> {
  @override
  void initState() {
    super.initState();
    // The accepted-response push may have been missed (app backgrounded/killed
    // when the customer accepted) — always refetch on open rather than trusting
    // only the live SignalR invalidate from home_screen.
    Future.microtask(() => ref.read(myServicesControllerProvider.notifier).refresh());
  }

  @override
  Widget build(BuildContext context) {
    final servicesAsync = ref.watch(myServicesControllerProvider);

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Mis servicios'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Pendientes'),
              Tab(text: 'Realizados'),
            ],
          ),
        ),
        body: SafeArea(
          child: servicesAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => Center(child: Text(describeApiError(error))),
            data: (services) {
              final pending = services.where((s) => s.status == 'Assigned').toList();
              final completed = services.where((s) => s.status == 'Completed').toList();
              return TabBarView(
                children: [
                  _ServicesList(
                    services: pending,
                    emptyMessage: 'No tienes servicios pendientes por realizar.',
                  ),
                  _ServicesList(
                    services: completed,
                    emptyMessage: 'Todavía no has completado ningún servicio.',
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ServicesList extends ConsumerWidget {
  const _ServicesList({required this.services, required this.emptyMessage});

  final List<ServiceRequest> services;
  final String emptyMessage;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (services.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            emptyMessage,
            style: Theme.of(context).textTheme.bodyMedium,
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(myServicesControllerProvider.notifier).refresh(),
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: services.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final service = services[index];
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(service.categoryName,
                            style: Theme.of(context).textTheme.titleLarge),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: requestStatusColor(context, service.status).withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          requestStatusLabel(service.status),
                          style: TextStyle(
                            color: requestStatusColor(context, service.status),
                            fontWeight: FontWeight.w600,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                  if (service.customerName != null) ...[
                    const SizedBox(height: 4),
                    Text('Cliente: ${service.customerName}',
                        style: Theme.of(context).textTheme.bodyMedium),
                  ],
                  const SizedBox(height: 6),
                  Text(service.description),
                  if (service.myResponse?.proposedPrice != null) ...[
                    const SizedBox(height: 8),
                    Text(
                      'Precio acordado: \$${service.myResponse!.proposedPrice!.toStringAsFixed(2)}',
                      style: Theme.of(context)
                          .textTheme
                          .titleMedium
                          ?.copyWith(color: Theme.of(context).colorScheme.primary),
                    ),
                  ],
                  const SizedBox(height: 6),
                  Text(timeAgo(service.createdAt), style: Theme.of(context).textTheme.bodyMedium),
                  if (service.status == 'Completed') ...[
                    const SizedBox(height: 10),
                    if (service.rating != null) ...[
                      Row(
                        children: [
                          ...List.generate(
                            5,
                            (index) => Icon(
                              index < service.rating! ? Icons.star_rounded : Icons.star_border_rounded,
                              color: Colors.amber,
                              size: 18,
                            ),
                          ),
                          const SizedBox(width: 6),
                          Text('Calificación del cliente',
                              style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                      if (service.reviewComment != null && service.reviewComment!.isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Text('"${service.reviewComment}"',
                            style: Theme.of(context)
                                .textTheme
                                .bodyMedium
                                ?.copyWith(fontStyle: FontStyle.italic)),
                      ],
                    ] else
                      Text(
                        'El cliente todavía no ha calificado este servicio.',
                        style: Theme.of(context)
                            .textTheme
                            .bodySmall
                            ?.copyWith(color: Theme.of(context).colorScheme.outline),
                      ),
                  ],
                  if (service.attachments.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    AttachmentsViewer(
                      requestId: service.id,
                      attachments: service.attachments,
                    ),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
