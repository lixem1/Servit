import 'package:servit_app/features/auth/domain/auth_session.dart';

class GoogleAuthResult {
  const GoogleAuthResult({
    required this.requiresRole,
    this.email,
    this.fullName,
    this.session,
  });

  final bool requiresRole;
  final String? email;
  final String? fullName;
  final AuthSession? session;

  factory GoogleAuthResult.fromJson(Map<String, dynamic> json) => GoogleAuthResult(
        requiresRole: json['requiresRole'] as bool,
        email: json['email'] as String?,
        fullName: json['fullName'] as String?,
        session: json['auth'] == null
            ? null
            : AuthSession.fromJson(json['auth'] as Map<String, dynamic>),
      );
}
