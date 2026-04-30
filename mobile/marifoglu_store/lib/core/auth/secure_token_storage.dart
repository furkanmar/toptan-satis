import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureTokenStorage {
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    // Web: localStorage kullanır (test için yeterli, production'da dikkat)
    webOptions: WebOptions(dbName: 'marifoglu_store', publicKey: 'marifoglu'),
  );

  static const _keyToken = 'auth_token';
  static const _keyRole = 'auth_role';
  static const _keyUserId = 'auth_user_id';
  static const _keyDisplayName = 'auth_display_name';

  static Future<void> saveToken(String token) =>
      _storage.write(key: _keyToken, value: token);

  static Future<String?> getToken() => _storage.read(key: _keyToken);

  static Future<void> saveRole(String role) =>
      _storage.write(key: _keyRole, value: role);

  static Future<String?> getRole() => _storage.read(key: _keyRole);

  static Future<void> saveUserId(String userId) =>
      _storage.write(key: _keyUserId, value: userId);

  static Future<String?> getUserId() => _storage.read(key: _keyUserId);

  static Future<void> saveDisplayName(String name) =>
      _storage.write(key: _keyDisplayName, value: name);

  static Future<String?> getDisplayName() =>
      _storage.read(key: _keyDisplayName);

  static Future<void> clearAll() => _storage.deleteAll();

  // Cart key per wholesaler
  static String cartKey(String wholesalerId) => 'cart_$wholesalerId';
}
