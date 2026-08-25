class AccountProfile {
  const AccountProfile({
    required this.fullName,
    required this.email,
    required this.role,
    required this.hasPassword,
  });

  final String fullName;
  final String email;
  final String role;
  final bool hasPassword;

  factory AccountProfile.fromJson(Map<String, dynamic> json) => AccountProfile(
        fullName: json['fullName'] as String,
        email: json['email'] as String,
        role: json['role'] as String,
        hasPassword: json['hasPassword'] as bool,
      );
}
