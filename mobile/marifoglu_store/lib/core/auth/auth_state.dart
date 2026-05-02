import 'package:flutter/foundation.dart';

@immutable
class AuthState {
  final String? token;
  final String? role;
  final String? userId;
  /// Store role → storeId, Wholesaler role → wholesalerId
  final String? profileId;
  final String? displayName;
  final bool isLoading;

  const AuthState({
    this.token,
    this.role,
    this.userId,
    this.profileId,
    this.displayName,
    this.isLoading = false,
  });

  bool get isAuthenticated => token != null;
  bool get isStore => role == 'Store';
  bool get isWholesaler => role == 'Wholesaler';

  AuthState copyWith({
    String? token,
    String? role,
    String? userId,
    String? profileId,
    String? displayName,
    bool? isLoading,
    bool clearToken = false,
  }) {
    return AuthState(
      token: clearToken ? null : (token ?? this.token),
      role: clearToken ? null : (role ?? this.role),
      userId: clearToken ? null : (userId ?? this.userId),
      profileId: clearToken ? null : (profileId ?? this.profileId),
      displayName: clearToken ? null : (displayName ?? this.displayName),
      isLoading: isLoading ?? this.isLoading,
    );
  }
}
