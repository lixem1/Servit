import 'package:servit_app/features/provider_profile/domain/review.dart';

class PublicProviderProfile {
  const PublicProviderProfile({
    required this.id,
    required this.fullName,
    this.bio,
    required this.averageRating,
    required this.ratingCount,
    required this.categoryNames,
    required this.reviews,
  });

  final String id;
  final String fullName;
  final String? bio;
  final double averageRating;
  final int ratingCount;
  final List<String> categoryNames;
  final List<Review> reviews;

  factory PublicProviderProfile.fromJson(Map<String, dynamic> json) => PublicProviderProfile(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        bio: json['bio'] as String?,
        averageRating: (json['averageRating'] as num).toDouble(),
        ratingCount: json['ratingCount'] as int,
        categoryNames: (json['categoryNames'] as List).map((e) => e as String).toList(),
        reviews: (json['reviews'] as List)
            .map((json) => Review.fromJson(json as Map<String, dynamic>))
            .toList(),
      );
}
