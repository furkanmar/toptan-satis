using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace WholesaleApi.Services.Storage;

public class R2StorageOptions
{
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;
}

public class R2FileStorage : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;

    public R2FileStorage(R2StorageOptions options)
    {
        _bucket = options.BucketName;

        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,   // R2 path-style zorunlu
            AuthenticationRegion = "auto"
        };
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task UploadAsync(Stream content, string key, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true   // R2 chunked signing desteklemiyor
        };
        await _client.PutObjectAsync(request);
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        var response = await _client.GetObjectAsync(_bucket, key);
        // Response stream'i memory'ye kopyala — bağlantıyı serbest bırak
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key)
    {
        await _client.DeleteObjectAsync(_bucket, key);
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry)
    {
        // Presigned URL hesaplama lokalde yapılır, network çağrısı yok
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS
        };
        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public bool Owns(string key) => key.StartsWith("products/", StringComparison.Ordinal);

    public void Dispose() => _client.Dispose();
}
