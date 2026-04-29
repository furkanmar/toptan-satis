import 'package:flutter/foundation.dart';

@immutable
class AuthState {
  final String? token;
  final String? role;
  final String? userId;
  final String? displayName;
  final bool isLoading;

  const AuthState({
    this.token,
    this.role,
    this.userId,
    this.displayName,
    this.isLoading = false,
  });

  bool get isAuthenticated => token != null;

  AuthState copyWith({
    String? token,
    String? role,
    String? userId,
    String? displayName,
    bool? isLoading,
    bool clearToken = false,
  }) {
    return AuthState(
      token: clearToken ? null : (token ?? this.token),
      role: clearToken ? null : (role ?? this.role),
      userId: clearToken ? null : (userId ?? this.userId),
      displayName: clearToken ? null : (displayName ?? this.displayName),
      isLoading: isLoading ?? this.isLoading,
    );
  }
}
