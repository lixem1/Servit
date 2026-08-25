class ProviderProfile {
  const ProviderProfile({
    required this.id,
    this.bio,
    this.lat,
    this.lng,
    required this.averageRating,
    required this.ratingCount,
    required this.categoryIds,
  });

  final String id;
  final String? bio;
  final double? lat;
  final double? lng;
  final double averageRating;
  final int ratingCount;
  final List<int> categoryIds;

  bool get hasLocation => lat != null && lng != null;

  factory ProviderProfile.fromJson(Map<String, dynamic> json) => ProviderProfile(
        id: json['id'] as String,
        bio: json['bio'] as String?,
        lat: (json['lat'] as num?)?.toDouble(),
        lng: (json['lng'] as num?)?.toDouble(),
        averageRating: (json['averageRating'] as num).toDouble(),
        ratingCount: json['ratingCount'] as int,
        categoryIds: (json['categoryIds'] as List).map((e) => e as int).toList(),
      );
}
