namespace WholesaleApi.Services.Storage;

/// <summary>
/// Legacy local dosya sistemi storage — migration tamamlanana kadar fallback.
/// Yeni upload yapmaz, sadece eski uploads/ dosyalarını okuma/silme destekler.
/// </summary>
public class LocalFileStorage : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _baseUrl;

    public LocalFileStorage(string webRootPath, string baseUrl)
    {
        _webRootPath = webRootPath;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task UploadAsync(Stream content, string key, string contentType)
        => throw new NotSupportedException("LocalFileStorage yeni upload desteklemiyor. R2FileStorage kullan.");

    public Task<Stream> DownloadAsync(string key)
    {
        var fullPath = Path.Combine(_webRootPath, key);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Dosya bulunamadı: {key}");
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = Path.Combine(_webRootPath, key);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry)
    {
        // Local dosyalar static URL üzerinden servis ediliyor, süre kavramı yok
        var url = $"{_baseUrl}/{key}";
        return Task.FromResult(url);
    }

    public bool Owns(string key) => key.StartsWith("uploads/", StringComparison.Ordinal);
}
