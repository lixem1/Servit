import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';
import 'package:servit_app/features/service_requests/presentation/status_labels.dart';
import 'package:servit_app/features/service_requests/presentation/widgets/attachments_viewer.dart';

class RequestDetailScreen extends ConsumerWidget {
  const RequestDetailScreen({super.key, required this.requestId, this.initialRequest});

  final String requestId;
  final ServiceRequest? initialRequest;

  Future<void> _confirmCancel(BuildContext context, WidgetRef ref, String requestId) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('¿Cancelar solicitud?'),
        content: const Text(
          'Se cancelará la solicitud y se rechazarán las respuestas pendientes de los proveedores.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('No'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Sí, cancelar'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await ref.read(myRequestsControllerProvider.notifier).cancel(requestId);
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Solicitud cancelada')));
      }
    } catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(describeApiError(error))));
      }
    }
  }

  ServiceRequest? _findRequest(WidgetRef ref) {
    final myRequests = ref.watch(myRequestsControllerProvider).valueOrNull;
    if (myRequests != null) {
      for (final request in myRequests) {
        if (request.id == requestId) return request;
      }
    }
    return initialRequest;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final responsesAsync = ref.watch(requestResponsesControllerProvider(requestId));
    final request = _findRequest(ref);

    ref.listen(requestResponsesControllerProvider(requestId), (previous, next) {
      final error = next.error;
      if (error != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(describeApiError(error))),
        );
      }
    });

    return Scaffold(
      appBar: AppBar(title: const Text('Solicitud')),
      body: SafeArea(
        child: responsesAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => Center(child: Text(describeApiError(error))),
          data: (responses) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (request != null) ...[
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(request.categoryName, style: Theme.of(context).textTheme.titleLarge),
                          const SizedBox(height: 8),
                          Text(request.description),
                          if (request.attachments.isNotEmpty) ...[
                            const SizedBox(height: 12),
                            AttachmentsViewer(requestId: requestId, attachments: request.attachments),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  if (request.status == 'Assigned')
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: FilledButton.icon(
                        onPressed: () async {
                          await ref.read(myRequestsControllerProvider.notifier).complete(requestId);
                        },
                        icon: const Icon(Icons.task_alt),
                        label: const Text('Marcar como completado'),
                      ),
                    ),
                  if (request.status == 'Pending' || request.status == 'Assigned')
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: OutlinedButton.icon(
                        onPressed: () => _confirmCancel(context, ref, requestId),
                        icon: const Icon(Icons.cancel_outlined),
                        label: const Text('Cancelar solicitud'),
                      ),
                    ),
                  if (request.status == 'Completed' && !request.hasReview)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: _ReviewForm(requestId: requestId),
                    ),
                ],
                if (responses.isEmpty)
                  Padding(
                    padding: const EdgeInsets.all(24),
                    child: Text(
                      'Todavía no hay respuestas de proveedores. Te avisaremos apenas llegue una.',
                      style: Theme.of(context).textTheme.bodyMedium,
                      textAlign: TextAlign.center,
                    ),
                  )
                else
                  ...responses.map((response) {
                    final isPending = response.status == 'Pending';
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              InkWell(
                                onTap: () => context.push('/providers/${response.providerId}'),
                                child: Row(
                                  children: [
                                    Expanded(
                                      child: Text(response.providerName,
                                          style: Theme.of(context).textTheme.titleLarge),
                                    ),
                                    Icon(Icons.star_rounded, color: Colors.amber, size: 18),
                                    const SizedBox(width: 2),
                                    Text(response.providerAverageRating.toStringAsFixed(1)),
                                    Text(' (${response.providerRatingCount})',
                                        style: Theme.of(context).textTheme.bodyMedium),
                                  ],
                                ),
                              ),
                              if (response.message != null && response.message!.isNotEmpty) ...[
                                const SizedBox(height: 8),
                                Text(response.message!),
                              ],
                              if (response.proposedPrice != null) ...[
                                const SizedBox(height: 8),
                                Text(
                                  'Propuesta: \$${response.proposedPrice!.toStringAsFixed(2)}',
                                  style: Theme.of(context)
                                      .textTheme
                                      .titleLarge
                                      ?.copyWith(color: Theme.of(context).colorScheme.primary),
                                ),
                              ],
                              const SizedBox(height: 12),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: requestStatusColor(context, response.status)
                                          .withValues(alpha: 0.12),
                                      borderRadius: BorderRadius.circular(20),
                                    ),
                                    child: Text(
                                      responseStatusLabel(response.status),
                                      style: TextStyle(
                                        color: requestStatusColor(context, response.status),
                                        fontWeight: FontWeight.w600,
                                        fontSize: 12,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              if (isPending) ...[
                                const SizedBox(height: 12),
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.end,
                                  children: [
                                    OutlinedButton(
                                      onPressed: () async {
                                        await ref
                                            .read(requestResponsesControllerProvider(requestId)
                                                .notifier)
                                            .reject(response.id);
                                      },
                                      child: const Text('Rechazar'),
                                    ),
                                    const SizedBox(width: 8),
                                    FilledButton(
                                      style: FilledButton.styleFrom(
                                        minimumSize: Size.zero,
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 16, vertical: 10),
                                      ),
                                      onPressed: () async {
                                        await ref
                                            .read(requestResponsesControllerProvider(requestId)
                                                .notifier)
                                            .accept(response.id);
                                      },
                                      child: const Text('Aceptar'),
                                    ),
                                  ],
                                ),
                              ],
                            ],
                          ),
                        ),
                      ),
                    );
                  }),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ReviewForm extends ConsumerStatefulWidget {
  const _ReviewForm({required this.requestId});

  final String requestId;

  @override
  ConsumerState<_ReviewForm> createState() => _ReviewFormState();
}

class _ReviewFormState extends ConsumerState<_ReviewForm> {
  int _rating = 5;
  final _commentController = TextEditingController();
  bool _submitting = false;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    try {
      await ref.read(myRequestsControllerProvider.notifier).submitReview(
            requestId: widget.requestId,
            rating: _rating,
            comment: _commentController.text.trim().isEmpty ? null : _commentController.text.trim(),
          );
      await ref.read(myRequestsControllerProvider.notifier).refresh();
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('¡Gracias por tu reseña!')));
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(describeApiError(error))),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Califica al proveedor', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Row(
              children: List.generate(5, (index) {
                final starIndex = index + 1;
                return IconButton(
                  onPressed: () => setState(() => _rating = starIndex),
                  icon: Icon(
                    starIndex <= _rating ? Icons.star_rounded : Icons.star_border_rounded,
                    color: Colors.amber,
                  ),
                );
              }),
            ),
            TextField(
              controller: _commentController,
              decoration: const InputDecoration(labelText: 'Comentario (opcional)'),
              maxLines: 3,
            ),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(
                      width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Enviar reseña'),
            ),
          ],
        ),
      ),
    );
  }
}
