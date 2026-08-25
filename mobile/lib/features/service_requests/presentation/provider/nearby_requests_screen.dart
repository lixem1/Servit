import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';
import 'package:servit_app/features/service_requests/presentation/status_labels.dart';
import 'package:servit_app/features/service_requests/presentation/widgets/attachments_viewer.dart';

class NearbyRequestsScreen extends ConsumerStatefulWidget {
  const NearbyRequestsScreen({super.key});

  @override
  ConsumerState<NearbyRequestsScreen> createState() => _NearbyRequestsScreenState();
}

class _NearbyRequestsScreenState extends ConsumerState<NearbyRequestsScreen> {
  @override
  void initState() {
    super.initState();
    // A push may have been missed while this screen was closed (app backgrounded,
    // categories just changed, etc.) — always refetch on open rather than trusting
    // only the live SignalR stream.
    Future.microtask(() => ref.read(nearbyRequestsControllerProvider.notifier).refresh());
  }

  @override
  Widget build(BuildContext context) {
    final requestsAsync = ref.watch(nearbyRequestsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Solicitudes cercanas')),
      body: SafeArea(
        child: requestsAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => Center(child: Text(describeApiError(error))),
          data: (requests) {
            if (requests.isEmpty) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(
                    'No hay solicitudes cerca de ti por ahora. Te avisaremos apenas llegue una.',
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                ),
              );
            }
            return RefreshIndicator(
              onRefresh: () => ref.read(nearbyRequestsControllerProvider.notifier).refresh(),
              child: ListView.separated(
                padding: const EdgeInsets.all(16),
                itemCount: requests.length,
                separatorBuilder: (context, index) => const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  final request = requests[index];
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(request.categoryName,
                                    style: Theme.of(context).textTheme.titleLarge),
                              ),
                              if (request.distanceMeters != null)
                                Text(
                                  formatDistance(request.distanceMeters),
                                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                        color: Theme.of(context).colorScheme.primary,
                                        fontWeight: FontWeight.w600,
                                      ),
                                ),
                            ],
                          ),
                          const SizedBox(height: 6),
                          Text(request.description),
                          const SizedBox(height: 6),
                          Text(timeAgo(request.createdAt),
                              style: Theme.of(context).textTheme.bodyMedium),
                          if (request.attachments.isNotEmpty) ...[
                            const SizedBox(height: 10),
                            AttachmentsViewer(
                              requestId: request.id,
                              attachments: request.attachments,
                            ),
                          ],
                          if (request.myResponse != null) ...[
                            const SizedBox(height: 10),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                              decoration: BoxDecoration(
                                color: request.myResponse!.status == 'Declined'
                                    ? Theme.of(context).colorScheme.errorContainer.withValues(alpha: 0.5)
                                    : Theme.of(context).colorScheme.primaryContainer.withValues(alpha: 0.4),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                request.myResponse!.status == 'Declined'
                                    ? 'Tu oferta fue rechazada. Envía una nueva propuesta.'
                                    : request.myResponse!.proposedPrice != null
                                        ? 'Tu oferta: \$${request.myResponse!.proposedPrice!.toStringAsFixed(2)}'
                                        : 'Ya enviaste una oferta',
                                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                      fontWeight: FontWeight.w600,
                                      color: request.myResponse!.status == 'Declined'
                                          ? Theme.of(context).colorScheme.error
                                          : null,
                                    ),
                              ),
                            ),
                          ],
                          const SizedBox(height: 12),
                          Align(
                            alignment: Alignment.centerRight,
                            child: FilledButton(
                              style: FilledButton.styleFrom(
                                minimumSize: Size.zero,
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 16, vertical: 10),
                              ),
                              onPressed: () => _showRespondSheet(context, ref, request),
                              child: Text(
                                request.myResponse == null
                                    ? 'Responder'
                                    : request.myResponse!.status == 'Declined'
                                        ? 'Enviar nueva oferta'
                                        : 'Actualizar oferta',
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            );
          },
        ),
      ),
    );
  }

  Future<void> _showRespondSheet(
    BuildContext context,
    WidgetRef ref,
    ServiceRequest request,
  ) async {
    final myResponse = request.myResponse;
    final messageController = TextEditingController(text: myResponse?.message ?? '');
    final priceController = TextEditingController(
      text: myResponse?.proposedPrice != null ? myResponse!.proposedPrice!.toStringAsFixed(2) : '',
    );
    var isSubmitting = false;

    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) {
        return StatefulBuilder(
          builder: (sheetContext, setSheetState) {
            return Padding(
              padding: EdgeInsets.only(
                left: 24,
                right: 24,
                top: 24,
                bottom: MediaQuery.of(sheetContext).viewInsets.bottom + 24,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                      myResponse != null
                          ? 'Actualizar oferta - ${request.categoryName}'
                          : 'Responder a ${request.categoryName}',
                      style: Theme.of(sheetContext).textTheme.headlineSmall),
                  const SizedBox(height: 16),
                  TextField(
                    controller: messageController,
                    maxLines: 3,
                    decoration: const InputDecoration(
                      labelText: 'Mensaje (opcional)',
                      prefixIcon: Icon(Icons.chat_bubble_outline),
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: priceController,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(
                      labelText: 'Precio propuesto (opcional)',
                      prefixIcon: Icon(Icons.attach_money),
                    ),
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: isSubmitting
                        ? null
                        : () async {
                            setSheetState(() => isSubmitting = true);
                            try {
                              await ref.read(serviceRequestsRepositoryProvider).respond(
                                    requestId: request.id,
                                    message: messageController.text.trim().isEmpty
                                        ? null
                                        : messageController.text.trim(),
                                    proposedPrice: double.tryParse(priceController.text.trim()),
                                  );
                              await ref.read(nearbyRequestsControllerProvider.notifier).refresh();
                              if (sheetContext.mounted) Navigator.of(sheetContext).pop();
                              if (context.mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(content: Text(myResponse != null ? 'Oferta actualizada' : 'Respuesta enviada')),
                                );
                              }
                            } catch (error) {
                              if (sheetContext.mounted) {
                                ScaffoldMessenger.of(sheetContext).showSnackBar(
                                  SnackBar(content: Text(describeApiError(error))),
                                );
                              }
                            } finally {
                              setSheetState(() => isSubmitting = false);
                            }
                          },
                    child: isSubmitting
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : Text(myResponse != null ? 'Actualizar oferta' : 'Enviar respuesta'),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}
