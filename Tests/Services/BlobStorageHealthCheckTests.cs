using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Services.HealthChecks;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class BlobStorageHealthCheckTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock = new();

    private BlobStorageHealthCheck CreateHealthCheck(BlobStorageOptions options)
    {
        return new BlobStorageHealthCheck(
            _s3ClientMock.Object,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBucketIsNotConfigured_ReturnsHealthyWithoutCallingS3()
    {
        var healthCheck = CreateHealthCheck(new BlobStorageOptions { BucketName = string.Empty });

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        _s3ClientMock.Verify(
            s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBucketIsReachable_ReturnsHealthy()
    {
        _s3ClientMock
            .Setup(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response());

        var healthCheck = CreateHealthCheck(new BlobStorageOptions { BucketName = "my-bucket" });

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        _s3ClientMock.Verify(
            s3 => s3.ListObjectsV2Async(
                It.Is<ListObjectsV2Request>(request =>
                    request.BucketName == "my-bucket" &&
                    request.MaxKeys == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenS3Throws_ReturnsUnhealthy()
    {
        _s3ClientMock
            .Setup(s3 => s3.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("R2 failure"));

        var healthCheck = CreateHealthCheck(new BlobStorageOptions { BucketName = "my-bucket" });

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description);
    }
}
