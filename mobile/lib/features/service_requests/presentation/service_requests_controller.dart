import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:servit_app/features/auth/presentation/auth_controller.dart';
import 'package:servit_app/features/provider_profile/presentation/provider_profile_controller.dart';
import 'package:servit_app/features/service_requests/data/realtime_service.dart';
import 'package:servit_app/features/service_requests/data/service_requests_repository.dart';
import 'package:servit_app/features/service_requests/domain/provider_response.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';

final serviceRequestsRepositoryProvider = Provider((ref) {
  return ServiceRequestsRepository(ref.read(apiClientProvider).dio);
});

final realtimeServiceProvider = Provider((ref) {
  final service = RealtimeService(
    baseUrl: ref.read(apiClientProvider).hubBaseUrl,
    tokenProvider: ref.read(authTokenProviderProvider),
  );
  ref.onDispose(service.disconnect);
  return service;
});

/// Requests visible to a provider ("nearby"), kept fresh via SignalR pushes.
final nearbyRequestsControllerProvider =
    AsyncNotifierProvider<NearbyRequestsController, List<ServiceRequest>>(
  NearbyRequestsController.new,
);

class NearbyRequestsController extends AsyncNotifier<List<ServiceRequest>> {
  @override
  Future<List<ServiceRequest>> build() async {
    try {
      await ref.read(providerProfileControllerProvider.notifier).refreshLocation();
    } catch (_) {
      // Location permission may be denied or unavailable — the provider can
      // still see nearby requests fetched below, just without live pushes
      // until their location is known.
    }

    final realtime = ref.read(realtimeServiceProvider);
    await realtime.connect();
    final createdSubscription = realtime.onRequestCreated.listen((request) {
      final current = state.valueOrNull ?? [];
      state = AsyncData([request, ...current]);
    });
    final unavailableSubscription = realtime.onRequestUnavailable.listen((requestId) {
      final current = state.valueOrNull ?? [];
      state = AsyncData(current.where((r) => r.id != requestId).toList());
    });
    final declinedSubscription = realtime.onResponseDeclined.listen((response) {
      final current = state.valueOrNull ?? [];
      final index = current.indexWhere((r) => r.id == response.serviceRequestId);
      if (index == -1) return;
      final updated = [...current];
      updated[index] = updated[index].copyWith(
        myResponse: MyResponseSummary(
          id: response.id,
          message: response.message,
          proposedPrice: response.proposedPrice,
          status: response.status,
          createdAt: response.createdAt,
        ),
      );
      state = AsyncData(updated);
    });
    ref.onDispose(createdSubscription.cancel);
    ref.onDispose(unavailableSubscription.cancel);
    ref.onDispose(declinedSubscription.cancel);
    return ref.read(serviceRequestsRepositoryProvider).getNearby();
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(() => ref.read(serviceRequestsRepositoryProvider).getNearby());
  }
}

/// Jobs a provider has been accepted for ("mis servicios" — pending + completed).
final myServicesControllerProvider =
    AsyncNotifierProvider<MyServicesController, List<ServiceRequest>>(
  MyServicesController.new,
);

class MyServicesController extends AsyncNotifier<List<ServiceRequest>> {
  @override
  Future<List<ServiceRequest>> build() async {
    return ref.read(serviceRequestsRepositoryProvider).getAssigned();
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(() => ref.read(serviceRequestsRepositoryProvider).getAssigned());
  }
}

/// Fires whenever one of the provider's offers gets accepted by a customer.
final responseAcceptedEventsProvider = StreamProvider<ProviderResponse>((ref) {
  return ref.watch(realtimeServiceProvider).onResponseAccepted;
});

/// Fires whenever one of the provider's offers gets rejected by a customer.
final responseDeclinedEventsProvider = StreamProvider<ProviderResponse>((ref) {
  return ref.watch(realtimeServiceProvider).onResponseDeclined;
});

/// Fires whenever a new request matching a provider's categories/location is created.
final requestCreatedEventsProvider = StreamProvider<ServiceRequest>((ref) {
  return ref.watch(realtimeServiceProvider).onRequestCreated;
});

/// Fires whenever a request the provider had a pending offer on is cancelled by the customer.
final serviceRequestCancelledEventsProvider = StreamProvider<CancelledRequestInfo>((ref) {
  return ref.watch(realtimeServiceProvider).onServiceRequestCancelled;
});

/// Fires whenever a customer's request receives a new quote from a provider.
final responseReceivedEventsProvider = StreamProvider<ProviderResponse>((ref) {
  return ref.watch(realtimeServiceProvider).onResponseReceived;
});

/// A customer's own requests, kept fresh via SignalR pushes on their responses.
final myRequestsControllerProvider =
    AsyncNotifierProvider<MyRequestsController, List<ServiceRequest>>(
  MyRequestsController.new,
);

class MyRequestsController extends AsyncNotifier<List<ServiceRequest>> {
  @override
  Future<List<ServiceRequest>> build() async {
    final realtime = ref.read(realtimeServiceProvider);
    await realtime.connect();
    return ref.read(serviceRequestsRepositoryProvider).getMine();
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(() => ref.read(serviceRequestsRepositoryProvider).getMine());
  }

  Future<ServiceRequest> create({
    required int categoryId,
    required String description,
    required double lat,
    required double lng,
    List<File> photos = const [],
    File? video,
    List<File> audios = const [],
  }) async {
    final request = await ref.read(serviceRequestsRepositoryProvider).create(
          categoryId: categoryId,
          description: description,
          lat: lat,
          lng: lng,
          photos: photos,
          video: video,
          audios: audios,
        );
    final current = state.valueOrNull ?? [];
    state = AsyncData([request, ...current]);
    return request;
  }

  Future<void> complete(String requestId) async {
    await ref.read(serviceRequestsRepositoryProvider).completeRequest(requestId);
    await refresh();
  }

  Future<void> cancel(String requestId) async {
    await ref.read(serviceRequestsRepositoryProvider).cancelRequest(requestId);
    await refresh();
  }

  Future<void> submitReview({
    required String requestId,
    required int rating,
    String? comment,
  }) async {
    await ref.read(serviceRequestsRepositoryProvider).submitReview(
          requestId: requestId,
          rating: rating,
          comment: comment,
        );
  }
}

/// Responses for a single request, kept fresh via SignalR pushes as providers respond.
final requestResponsesControllerProvider = AsyncNotifierProvider.family<
    RequestResponsesController, List<ProviderResponse>, String>(
  RequestResponsesController.new,
);

class RequestResponsesController
    extends FamilyAsyncNotifier<List<ProviderResponse>, String> {
  @override
  Future<List<ProviderResponse>> build(String arg) async {
    final realtime = ref.read(realtimeServiceProvider);
    await realtime.connect();
    final subscription = realtime.onResponseReceived.listen((response) {
      if (response.serviceRequestId != arg) return;
      final current = state.valueOrNull ?? [];
      final existingIndex = current.indexWhere((r) => r.id == response.id);
      if (existingIndex == -1) {
        state = AsyncData([response, ...current]);
      } else {
        final updated = [...current];
        updated[existingIndex] = response;
        state = AsyncData(updated);
      }
    });
    ref.onDispose(subscription.cancel);
    return ref.read(serviceRequestsRepositoryProvider).getResponses(arg);
  }

  Future<void> respond({String? message, double? proposedPrice}) async {
    await ref.read(serviceRequestsRepositoryProvider).respond(
          requestId: arg,
          message: message,
          proposedPrice: proposedPrice,
        );
  }

  Future<void> accept(String responseId) async {
    await ref.read(serviceRequestsRepositoryProvider).acceptResponse(
          requestId: arg,
          responseId: responseId,
        );
    state = await AsyncValue.guard(
      () => ref.read(serviceRequestsRepositoryProvider).getResponses(arg),
    );
    await ref.read(myRequestsControllerProvider.notifier).refresh();
  }

  Future<void> reject(String responseId) async {
    await ref.read(serviceRequestsRepositoryProvider).rejectResponse(
          requestId: arg,
          responseId: responseId,
        );
    state = await AsyncValue.guard(
      () => ref.read(serviceRequestsRepositoryProvider).getResponses(arg),
    );
  }
}
