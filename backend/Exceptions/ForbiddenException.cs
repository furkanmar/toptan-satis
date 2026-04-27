namespace WholesaleApi.Exceptions;

/// <summary>
/// Kimlik doğrulandı ama bu kaynağa erişim yetkisi yok → HTTP 403 Forbidden
/// </summary>
public class ForbiddenException(string message) : Exception(message);
