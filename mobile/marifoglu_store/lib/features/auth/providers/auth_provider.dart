import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../../core/api/dio_client.dart';
import '../../../core/auth/auth_state.dart';
import '../../../core/auth/secure_token_storage.dart';
import '../models/auth_response.dart';

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});

/// Global Dio instance — auth provider'dan sonra init edilir.
final dioProvider = Provider<Dio>((ref) {
  final notifier = ref.read(authProvider.notifier);
  return DioClient(onUnauthorized: notifier.logout).dio;
});

class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier() : super(const AuthState()) {
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    final token = await SecureTokenStorage.getToken();
    if (token == null) return;

    final role = await SecureTokenStorage.getRole();
    final userId = await SecureTokenStorage.getUserId();
    final profileId = await SecureTokenStorage.getProfileId();
    final displayName = await SecureTokenStorage.getDisplayName();

    state = AuthState(
      token: token,
      role: role,
      userId: userId,
      profileId: profileId,
      displayName: displayName,
    );
  }

  Future<void> login(String email, String password) async {
    state = state.copyWith(isLoading: true);
    try {
      final dio = DioClient().dio;
      final response = await dio.post(
        ApiEndpoints.login,
        data: {'email': email, 'password': password},
      );
      final auth = AuthResponse.fromJson(response.data as Map<String, dynamic>);

      await SecureTokenStorage.saveToken(auth.token);
      await SecureTokenStorage.saveRole(auth.role);
      await SecureTokenStorage.saveUserId(auth.userId);
      if (auth.profileId != null) {
        await SecureTokenStorage.saveProfileId(auth.profileId!);
      }
      await SecureTokenStorage.saveDisplayName(auth.displayName);

      state = AuthState(
        token: auth.token,
        role: auth.role,
        userId: auth.userId,
        profileId: auth.profileId,
        displayName: auth.displayName,
      );
    } catch (_) {
      state = state.copyWith(isLoading: false);
      rethrow;
    }
  }

  Future<void> logout() async {
    await SecureTokenStorage.clearAll();
    state = const AuthState();
  }
}
