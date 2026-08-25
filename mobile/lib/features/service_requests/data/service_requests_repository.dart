import 'dart:io';

import 'package:dio/dio.dart';
import 'package:servit_app/features/service_requests/domain/provider_response.dart';
import 'package:servit_app/features/service_requests/domain/service_request.dart';

class ServiceRequestsRepository {
  ServiceRequestsRepository(this._dio);

  final Dio _dio;

  Future<ServiceRequest> create({
    required int categoryId,
    required String description,
    required double lat,
    required double lng,
    List<File> photos = const [],
    File? video,
    List<File> audios = const [],
  }) async {
    final formData = FormData.fromMap({
      'categoryId': categoryId,
      'description': description,
      'lat': lat,
      'lng': lng,
      if (photos.isNotEmpty)
        'photos': [for (final photo in photos) await _multipartFor(photo, 'photo')],
      if (video != null) 'video': await _multipartFor(video, 'video'),
      if (audios.isNotEmpty)
        'audios': [for (final audio in audios) await _multipartFor(audio, 'audio')],
    });

    final response = await _dio.post('/service-requests', data: formData);
    return ServiceRequest.fromJson(response.data as Map<String, dynamic>);
  }

  Future<MultipartFile> _multipartFor(File file, String kind) {
    return MultipartFile.fromFile(
      file.path,
      filename: file.path.split('/').last,
      contentType: DioMediaType.parse(_contentTypeFor(file.path, kind)),
    );
  }

  String _contentTypeFor(String path, String kind) {
    final extension = path.split('.').last.toLowerCase();
    switch (extension) {
      case 'jpg':
      case 'jpeg':
        return 'image/jpeg';
      case 'png':
        return 'image/png';
      case 'heic':
      case 'heif':
        return 'image/heic';
      case 'mp4':
        return kind == 'audio' ? 'audio/mp4' : 'video/mp4';
      case 'mov':
        return 'video/quicktime';
      case 'm4a':
        return 'audio/mp4';
      case 'mp3':
        return 'audio/mpeg';
      case 'wav':
        return 'audio/wav';
      default:
        return 'application/octet-stream';
    }
  }

  Future<List<ServiceRequest>> getNearby() async {
    final response = await _dio.get('/service-requests/nearby');
    return (response.data as List)
        .map((json) => ServiceRequest.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<List<ServiceRequest>> getMine() async {
    final response = await _dio.get('/service-requests/mine');
    return (response.data as List)
        .map((json) => ServiceRequest.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<List<ServiceRequest>> getAssigned() async {
    final response = await _dio.get('/service-requests/assigned');
    return (response.data as List)
        .map((json) => ServiceRequest.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<ProviderResponse> respond({
    required String requestId,
    String? message,
    double? proposedPrice,
  }) async {
    final response = await _dio.post('/service-requests/$requestId/responses', data: {
      'message': message,
      'proposedPrice': proposedPrice,
    });
    return ProviderResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<List<ProviderResponse>> getResponses(String requestId) async {
    final response = await _dio.get('/service-requests/$requestId/responses');
    return (response.data as List)
        .map((json) => ProviderResponse.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Future<void> acceptResponse({required String requestId, required String responseId}) {
    return _dio.post('/service-requests/$requestId/responses/$responseId/accept');
  }

  Future<void> rejectResponse({required String requestId, required String responseId}) {
    return _dio.post('/service-requests/$requestId/responses/$responseId/reject');
  }

  Future<void> completeRequest(String requestId) {
    return _dio.post('/service-requests/$requestId/complete');
  }

  Future<void> cancelRequest(String requestId) {
    return _dio.post('/service-requests/$requestId/cancel');
  }

  Future<void> submitReview({
    required String requestId,
    required int rating,
    String? comment,
  }) {
    return _dio.post('/service-requests/$requestId/review', data: {
      'rating': rating,
      'comment': comment,
    });
  }

  Future<List<int>> downloadAttachment({
    required String requestId,
    required String attachmentId,
  }) async {
    final response = await _dio.get<List<int>>(
      '/service-requests/$requestId/attachments/$attachmentId',
      options: Options(responseType: ResponseType.bytes),
    );
    return response.data!;
  }
}
