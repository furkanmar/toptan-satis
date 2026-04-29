import 'package:dio/dio.dart';
import 'package:uuid/uuid.dart';

/// POST / PUT / PATCH isteklere otomatik Idempotency-Key ekler.
/// Retry durumunda aynı key kullanılır (extra["idempotency_key"] ile taşınır).
class IdempotencyInterceptor extends Interceptor {
  static const _uuid = Uuid();
  static const _mutatingMethods = {'POST', 'PUT', 'PATCH'};

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (_mutatingMethods.contains(options.method.toUpperCase())) {
      // Retry'da aynı key'i koru
      final existingKey = options.extra['idempotency_key'] as String?;
      final key = existingKey ?? _uuid.v4();
      options.extra['idempotency_key'] = key;
      options.headers['Idempotency-Key'] = key;
    }
    handler.next(options);
  }
}
