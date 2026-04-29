import 'package:dio/dio.dart';
import 'interceptors/auth_interceptor.dart';
import 'interceptors/error_interceptor.dart';
import 'interceptors/idempotency_interceptor.dart';

/// --dart-define=BASE_URL=https://api.marifoglu.trade
const _baseUrl = String.fromEnvironment(
  'BASE_URL',
  defaultValue: 'http://10.0.2.2:5000', // Android emülatör → localhost
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
