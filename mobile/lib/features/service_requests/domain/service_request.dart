import 'package:servit_app/features/service_requests/domain/attachment.dart';

class MyResponseSummary {
  const MyResponseSummary({
    required this.id,
    this.message,
    this.proposedPrice,
    required this.status,
    required this.createdAt,
  });

  final String id;
  final String? message;
  final double? proposedPrice;
  final String status;
  final DateTime createdAt;

  factory MyResponseSummary.fromJson(Map<String, dynamic> json) => MyResponseSummary(
        id: json['id'] as String,
        message: json['message'] as String?,
        proposedPrice: (json['proposedPrice'] as num?)?.toDouble(),
        status: json['status'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

class ServiceRequest {
  const ServiceRequest({
    required this.id,
    required this.categoryId,
    required this.categoryName,
    required this.description,
    required this.lat,
    required this.lng,
    required this.status,
    required this.createdAt,
    this.distanceMeters,
    required this.attachments,
    required this.hasReview,
    this.responseCount = 0,
    this.myResponse,
    this.customerName,
    this.rating,
    this.reviewComment,
  });

  final String id;
  final int categoryId;
  final String categoryName;
  final String description;
  final double lat;
  final double lng;
  final String status;
  final DateTime createdAt;
  final double? distanceMeters;
  final List<Attachment> attachments;
  final bool hasReview;
  final int responseCount;
  final MyResponseSummary? myResponse;
  final String? customerName;
  final int? rating;
  final String? reviewComment;

  factory ServiceRequest.fromJson(Map<String, dynamic> json) => ServiceRequest(
        id: json['id'] as String,
        categoryId: json['categoryId'] as int,
        categoryName: json['categoryName'] as String,
        description: json['description'] as String,
        lat: (json['lat'] as num).toDouble(),
        lng: (json['lng'] as num).toDouble(),
        status: json['status'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
        distanceMeters: (json['distanceMeters'] as num?)?.toDouble(),
        attachments: (json['attachments'] as List? ?? [])
            .map((json) => Attachment.fromJson(json as Map<String, dynamic>))
            .toList(),
        hasReview: json['hasReview'] as bool? ?? false,
        responseCount: json['responseCount'] as int? ?? 0,
        myResponse: json['myResponse'] == null
            ? null
            : MyResponseSummary.fromJson(json['myResponse'] as Map<String, dynamic>),
        customerName: json['customerName'] as String?,
        rating: json['rating'] as int?,
        reviewComment: json['reviewComment'] as String?,
      );

  ServiceRequest copyWith({MyResponseSummary? myResponse}) => ServiceRequest(
        id: id,
        categoryId: categoryId,
        categoryName: categoryName,
        description: description,
        lat: lat,
        lng: lng,
        status: status,
        createdAt: createdAt,
        distanceMeters: distanceMeters,
        attachments: attachments,
        hasReview: hasReview,
        responseCount: responseCount,
        myResponse: myResponse ?? this.myResponse,
        customerName: customerName,
        rating: rating,
        reviewComment: reviewComment,
      );
}
