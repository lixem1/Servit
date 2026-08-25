import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/core/network/api_error.dart';
import 'package:servit_app/features/provider_profile/presentation/provider_profile_controller.dart';
import 'package:servit_app/features/service_requests/presentation/status_labels.dart';

class ProviderProfileViewScreen extends ConsumerWidget {
  const ProviderProfileViewScreen({super.key, required this.providerId});

  final String providerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(publicProviderProfileProvider(providerId));

    return Scaffold(
      appBar: AppBar(title: const Text('Perfil del proveedor')),
      body: SafeArea(
        child: profileAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => Center(child: Text(describeApiError(error))),
          data: (profile) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(profile.fullName, style: Theme.of(context).textTheme.headlineSmall),
                        const SizedBox(height: 8),
                        Row(
                          children: [
                            const Icon(Icons.star_rounded, color: Colors.amber, size: 20),
                            const SizedBox(width: 4),
                            Text(
                              profile.averageRating.toStringAsFixed(1),
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                            Text(' (${profile.ratingCount} reseñas)',
                                style: Theme.of(context).textTheme.bodyMedium),
                          ],
                        ),
                        if (profile.bio != null && profile.bio!.isNotEmpty) ...[
                          const SizedBox(height: 12),
                          Text(profile.bio!),
                        ],
                        if (profile.categoryNames.isNotEmpty) ...[
                          const SizedBox(height: 12),
                          Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children: profile.categoryNames
                                .map((name) => Chip(label: Text(name)))
                                .toList(),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Text('Reseñas', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                if (profile.reviews.isEmpty)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    child: Text(
                      'Este proveedor todavía no tiene reseñas.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  )
                else
                  ...profile.reviews.map((review) => Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(review.customerName,
                                        style: Theme.of(context).textTheme.titleMedium),
                                  ),
                                  Text(timeAgo(review.createdAt),
                                      style: Theme.of(context).textTheme.bodySmall),
                                ],
                              ),
                              const SizedBox(height: 4),
                              Row(
                                children: List.generate(
                                  5,
                                  (index) => Icon(
                                    index < review.rating ? Icons.star_rounded : Icons.star_border_rounded,
                                    color: Colors.amber,
                                    size: 16,
                                  ),
                                ),
                              ),
                              if (review.comment != null && review.comment!.isNotEmpty) ...[
                                const SizedBox(height: 8),
                                Text(review.comment!),
                              ],
                            ],
                          ),
                        ),
                      )),
              ],
            );
          },
        ),
      ),
    );
  }
}
