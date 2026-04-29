class AppException implements Exception {
  final String message;
  final int? statusCode;

  const AppException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class UnauthorizedException extends AppException {
  const UnauthorizedException() : super('Oturum süresi doldu', statusCode: 401);
}

class ForbiddenException extends AppException {
  const ForbiddenException() : super('Bu işlemi yapma yetkiniz yok', statusCode: 403);
}

class NetworkException extends AppException {
  const NetworkException() : super('Bağlantı hatası. İnternet bağlantınızı kontrol edin.');
}

class ServerException extends AppException {
  const ServerException([String msg = 'Sunucu hatası. Lütfen tekrar deneyin.'])
      : super(msg, statusCode: 500);
}

class ConflictException extends AppException {
  const ConflictException([String msg = 'İşlem çakışması. Tekrar deneyin.'])
      : super(msg, statusCode: 409);
}

class ValidationException extends AppException {
  const ValidationException(String msg) : super(msg, statusCode: 422);
}
