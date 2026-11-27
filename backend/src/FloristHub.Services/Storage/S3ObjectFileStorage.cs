using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FloristHub.Services.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FloristHub.Services.Storage;

/// <summary>
/// Armazenamento compatível com S3 (Cloudflare R2, AWS, etc.).
/// </summary>
public class S3ObjectFileStorage : IFileStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;
    private readonly ILogger<S3ObjectFileStorage> _logger;

    public S3ObjectFileStorage(IConfiguration configuration, ILogger<S3ObjectFileStorage> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("Storage:S3");
        _bucket = section["Bucket"] ?? throw new InvalidOperationException("Storage:S3:Bucket em falta.");
        _publicBaseUrl = (section["PublicBaseUrl"] ?? throw new InvalidOperationException("Storage:S3:PublicBaseUrl em falta."))
            .TrimEnd('/');

        var serviceUrl = section["ServiceUrl"] ?? throw new InvalidOperationException("Storage:S3:ServiceUrl em falta.");
        var accessKey = section["AccessKeyId"] ?? Environment.GetEnvironmentVariable("Storage__S3__AccessKeyId")
            ?? throw new InvalidOperationException("Storage:S3:AccessKeyId em falta.");
        var secretKey = section["SecretAccessKey"] ?? Environment.GetEnvironmentVariable("Storage__S3__SecretAccessKey")
            ?? throw new InvalidOperationException("Storage:S3:SecretAccessKey em falta.");

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = section.GetValue("ForcePathStyle", true),
            AuthenticationRegion = section["Region"] ?? "auto",
            // O R2 rejeita o checksum por omissão do AWS SDK 4. Só assinar quando a operação o exige.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        _client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<StoredFileDescriptor> SaveAsync(
        Stream content,
        string contentType,
        string originalFileName,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
            ext = GuessExtension(contentType);

        var key = $"{tenantId:N}/{Guid.NewGuid():N}{ext}";

        long size;
        if (content.CanSeek)
        {
            size = content.Length;
        }
        else
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            size = buffer.Length;
            buffer.Position = 0;
            content = buffer;
        }

        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await _client.PutObjectAsync(put, ct);
        var publicUrl = $"{_publicBaseUrl}/{key}";
        _logger.LogInformation("Objeto gravado no bucket {Bucket}: {Key}", _bucket, key);
        return new StoredFileDescriptor(key, publicUrl, contentType, size);
    }

    public async Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, storagePath, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void DeleteIfExists(string storagePath)
    {
        try
        {
            _client.DeleteObjectAsync(_bucket, storagePath).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível apagar {Key}", storagePath);
        }
    }

    private static string GuessExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".bin"
    };
}
