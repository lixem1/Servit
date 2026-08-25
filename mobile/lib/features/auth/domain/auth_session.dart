class AuthSession {
  const AuthSession({
    required this.token,
    required this.expiresAt,
    required this.userId,
    required this.fullName,
    required this.role,
  });

  final String token;
  final DateTime expiresAt;
  final String userId;
  final String fullName;
  final String role;

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
        userId: json['userId'] as String,
        fullName: json['fullName'] as String,
        role: json['role'] as String,
      );

  Map<String, dynamic> toJson() => {
        'token': token,
        'expiresAt': expiresAt.toIso8601String(),
        'userId': userId,
        'fullName': fullName,
        'role': role,
      };
}
