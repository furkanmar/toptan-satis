namespace WholesaleApi.Entities;

/// <summary>
/// Bu attribute ile işaretlenen entity'lerin Modified değişiklikleri
/// AuditInterceptor tarafından otomatik olarak AuditLog'a yazılır.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditableAttribute : Attribute;
