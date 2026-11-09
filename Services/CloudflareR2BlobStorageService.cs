using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class CloudflareR2BlobStorageService : IBlobStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<CloudflareR2BlobStorageService> _logger;

    public CloudflareR2BlobStorageService(
        IAmazonS3 s3Client,
        IOptions<BlobStorageOptions> options,
        ILogger<CloudflareR2BlobStorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string objectKey = $"profiles/{Guid.NewGuid()}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(putRequest, ct);

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.BucketName}/{objectKey}";
        }

        return $"https://{_options.BucketName}.r2.cloudflarestorage.com/{objectKey}";
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        try
        {
            string objectKey = uri.AbsolutePath.TrimStart('/');

            if (!string.IsNullOrWhiteSpace(_options.BucketName) &&
                objectKey.StartsWith(_options.BucketName + "/", StringComparison.OrdinalIgnoreCase))
            {
                objectKey = objectKey.Substring(_options.BucketName.Length + 1);
            }

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob object at {FileUrl}", fileUrl);
        }
    }
}
