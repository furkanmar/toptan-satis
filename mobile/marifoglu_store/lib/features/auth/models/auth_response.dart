class AuthResponse {
  final String token;
  final String role;
  final String userId;
  final String? profileId;
  final String displayName;

  const AuthResponse({
    required this.token,
    required this.role,
    required this.userId,
    this.profileId,
    required this.displayName,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        token: json['token'] as String,
        role: json['role'] as String,
        userId: json['userId'].toString(),
        profileId: json['profileId']?.toString(),
        displayName: json['displayName'] as String,
      );
}
