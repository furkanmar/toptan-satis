namespace WholesaleApi.Services.Storage;

public interface IFileStorageService
{
    /// <summary>
    /// Dosyayı storage'a yükle. Key'i caller belirler (örn. "products/{productId}/{guid}.webp").
    /// </summary>
    Task UploadAsync(Stream content, string key, string contentType);

    /// <summary>
    /// Dosyayı stream olarak indir. Migration script için.
    /// </summary>
    Task<Stream> DownloadAsync(string key);

    /// <summary>
    /// Dosyayı sil.
    /// </summary>
    Task DeleteAsync(string key);

    /// <summary>
    /// Geçici erişim URL'i üret (R2 → presigned, local → static URL).
    /// </summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry);

    /// <summary>
    /// Bu path/key bu storage'a ait mi? (legacy local path'leri ayırt için)
    /// </summary>
    bool Owns(string key);
}
