class ProviderResponse {
  const ProviderResponse({
    required this.id,
    required this.serviceRequestId,
    required this.providerId,
    required this.providerName,
    required this.providerAverageRating,
    required this.providerRatingCount,
    this.message,
    this.proposedPrice,
    required this.status,
    required this.createdAt,
  });

  final String id;
  final String serviceRequestId;
  final String providerId;
  final String providerName;
  final double providerAverageRating;
  final int providerRatingCount;
  final String? message;
  final double? proposedPrice;
  final String status;
  final DateTime createdAt;

  factory ProviderResponse.fromJson(Map<String, dynamic> json) => ProviderResponse(
        id: json['id'] as String,
        serviceRequestId: json['serviceRequestId'] as String,
        providerId: json['providerId'] as String,
        providerName: json['providerName'] as String,
        providerAverageRating: (json['providerAverageRating'] as num).toDouble(),
        providerRatingCount: json['providerRatingCount'] as int,
        message: json['message'] as String?,
        proposedPrice: (json['proposedPrice'] as num?)?.toDouble(),
        status: json['status'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
