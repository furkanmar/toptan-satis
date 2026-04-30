import 'package:dio/dio.dart';
import 'interceptors/auth_interceptor.dart';
import 'interceptors/error_interceptor.dart';
import 'interceptors/idempotency_interceptor.dart';

/// Prod build: --dart-define=BASE_URL=https://api.marifoglu.trade
/// Local emulator override: --dart-define=BASE_URL=http://10.0.2.2:5000
const _baseUrl = String.fromEnvironment(
  'BASE_URL',
  defaultValue: 'https://api.marifoglu.trade',
);

class DioClient {
  late final Dio _dio;

  DioClient({Future<void> Function()? onUnauthorized}) {
    _dio = Dio(
      BaseOptions(
        baseUrl: '$_baseUrl/api',
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    _dio.interceptors.addAll([
      AuthInterceptor(),
      IdempotencyInterceptor(),
      ErrorInterceptor(onUnauthorized: onUnauthorized),
      LogInterceptor(
        requestBody: false,
        responseBody: false,
        logPrint: (o) => null, // prod'da sustur
      ),
    ]);
  }

  Dio get dio => _dio;
}
