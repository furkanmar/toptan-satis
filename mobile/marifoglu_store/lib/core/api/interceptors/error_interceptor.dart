import 'package:dio/dio.dart';
import '../../errors/app_exception.dart';

class ErrorInterceptor extends Interceptor {
  /// 401 gelince çağrılır — logout için dışarıdan inject edilir.
  final Future<void> Function()? onUnauthorized;

  ErrorInterceptor({this.onUnauthorized});

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    if (err.type == DioExceptionType.connectionError ||
        err.type == DioExceptionType.connectionTimeout ||
        err.type == DioExceptionType.receiveTimeout) {
      return handler.reject(
        DioException(
          requestOptions: err.requestOptions,
          error: const NetworkException(),
          type: err.type,
        ),
      );
    }

    final statusCode = err.response?.statusCode;
    switch (statusCode) {
      case 401:
        await onUnauthorized?.call();
        return handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            error: const UnauthorizedException(),
            type: DioExceptionType.badResponse,
          ),
        );
      case 403:
        return handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            error: const ForbiddenException(),
            type: DioExceptionType.badResponse,
          ),
        );
      case 409:
        return handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            error: const ConflictException(),
            type: DioExceptionType.badResponse,
          ),
        );
      case 422:
      case 400:
        final msg = _extractMessage(err.response?.data) ??
            'Geçersiz istek. Lütfen bilgileri kontrol edin.';
        return handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            error: ValidationException(msg),
            type: DioExceptionType.badResponse,
          ),
        );
      case 500:
      default:
        return handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            error: const ServerException(),
            type: DioExceptionType.badResponse,
          ),
        );
    }
  }

  String? _extractMessage(dynamic data) {
    if (data == null) return null;
    if (data is Map) {
      return data['message']?.toString() ??
          data['title']?.toString() ??
          data['errors']?.toString();
    }
    return data.toString();
  }
}
