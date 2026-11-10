using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SocialMediaAppBackend.Options;

namespace SocialMediaAppBackend.Services.HealthChecks;

public class BlobStorageHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private readonly IAmazonS3 _s3Client;
    private readonly BlobStorageOptions _options;

    public BlobStorageHealthCheck(IAmazonS3 s3Client, IOptions<BlobStorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            return HealthCheckResult.Healthy("Blob storage is not configured.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        try
        {
            await _s3Client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _options.BucketName,
                    MaxKeys = 1
                },
                timeoutCts.Token);

            return HealthCheckResult.Healthy("Blob storage is reachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Blob storage health check timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage is unreachable.", ex);
        }
    }
}
