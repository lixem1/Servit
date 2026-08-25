import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:servit_app/features/service_requests/domain/provider_response.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';
import 'package:signalr_netcore/signalr_client.dart';

typedef TokenProvider = Future<String?> Function();

class CancelledRequestInfo {
  const CancelledRequestInfo({required this.requestId, required this.categoryName});

  final String requestId;
  final String categoryName;

  factory CancelledRequestInfo.fromJson(Map<String, dynamic> json) {
    return CancelledRequestInfo(
      requestId: json['requestId'].toString(),
      categoryName: json['categoryName'] as String? ?? '',
    );
  }
}

class RealtimeService {
  RealtimeService({required this.baseUrl, required this.tokenProvider});

  final String baseUrl;
  final TokenProvider tokenProvider;

  HubConnection? _connection;

  final _requestCreatedController = StreamController<ServiceRequest>.broadcast();
  final _responseReceivedController = StreamController<ProviderResponse>.broadcast();
  final _responseAcceptedController = StreamController<ProviderResponse>.broadcast();
  final _responseDeclinedController = StreamController<ProviderResponse>.broadcast();
  final _requestUnavailableController = StreamController<String>.broadcast();
  final _requestCancelledController = StreamController<CancelledRequestInfo>.broadcast();

  Stream<ServiceRequest> get onRequestCreated => _requestCreatedController.stream;
  Stream<ProviderResponse> get onResponseReceived => _responseReceivedController.stream;
  Stream<ProviderResponse> get onResponseAccepted => _responseAcceptedController.stream;
  Stream<ProviderResponse> get onResponseDeclined => _responseDeclinedController.stream;
  Stream<String> get onRequestUnavailable => _requestUnavailableController.stream;
  Stream<CancelledRequestInfo> get onServiceRequestCancelled => _requestCancelledController.stream;

  Future<void> connect() async {
    if (_connection != null) return;

    final connection = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/hubs/service-requests',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => (await tokenProvider()) ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.on('ServiceRequestCreated', (arguments) {
      final json = arguments?[0] as Map<String, dynamic>?;
      if (json != null) _requestCreatedController.add(ServiceRequest.fromJson(json));
    });
    connection.on('ResponseReceived', (arguments) {
      final json = arguments?[0] as Map<String, dynamic>?;
      if (json != null) _responseReceivedController.add(ProviderResponse.fromJson(json));
    });
    connection.on('ResponseAccepted', (arguments) {
      final json = arguments?[0] as Map<String, dynamic>?;
      if (json != null) _responseAcceptedController.add(ProviderResponse.fromJson(json));
    });
    connection.on('ResponseDeclined', (arguments) {
      final json = arguments?[0] as Map<String, dynamic>?;
      if (json != null) _responseDeclinedController.add(ProviderResponse.fromJson(json));
    });
    connection.on('ServiceRequestUnavailable', (arguments) {
      final id = arguments?[0] as String?;
      if (id != null) _requestUnavailableController.add(id);
    });
    connection.on('ServiceRequestCancelled', (arguments) {
      final json = arguments?[0] as Map<String, dynamic>?;
      if (json != null) _requestCancelledController.add(CancelledRequestInfo.fromJson(json));
    });

    _connection = connection;
    try {
      await connection.start();
    } catch (error) {
      debugPrint('SignalR connection failed: $error');
    }
  }

  Future<void> disconnect() async {
    await _connection?.stop();
    _connection = null;
  }
}
