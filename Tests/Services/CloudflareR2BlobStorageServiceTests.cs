using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Services;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class CloudflareR2BlobStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock = new();
    private readonly Mock<ILogger<CloudflareR2BlobStorageService>> _loggerMock = new();

    private CloudflareR2BlobStorageService CreateService(BlobStorageOptions options)
    {
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        return new CloudflareR2BlobStorageService(_s3ClientMock.Object, optionsWrapper, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadAsync_WithPublicBaseUrl_ReturnsPublicCdnUrl()
    {
        var options = new BlobStorageOptions
        {
            BucketName = "my-bucket",
            PublicBaseUrl = "https://cdn.example.com"
        };
        var service = CreateService(options);

        using var stream = new MemoryStream([0x01, 0x02]);
        string url = await service.UploadAsync(stream, "test.png", "image/png");

        Assert.StartsWith("https://cdn.example.com/profiles/", url);
        Assert.EndsWith(".png", url);

        _s3ClientMock.Verify(s3 => s3.PutObjectAsync(
            It.Is<PutObjectRequest>(req =>
                req.BucketName == "my-bucket" &&
                req.ContentType == "image/png" &&
                req.DisablePayloadSigning == true &&
                req.Key.StartsWith("profiles/") &&
                req.Key.EndsWith(".png")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithServiceUrl_ReturnsPathStyleUrl()
    {
        var options = new BlobStorageOptions
        {
            BucketName = "my-bucket",
            ServiceUrl = "https://accountid.r2.cloudflarestorage.com"
        };
        var service = CreateService(options);

        using var stream = new MemoryStream([0x01, 0x02]);
        string url = await service.UploadAsync(stream, "test.jpg", "image/jpeg");

        Assert.StartsWith("https://accountid.r2.cloudflarestorage.com/my-bucket/profiles/", url);
        Assert.EndsWith(".jpg", url);
    }

    [Fact]
    public async Task UploadAsync_WithoutPublicBaseUrlOrServiceUrl_ReturnsDefaultR2Url()
    {
        var options = new BlobStorageOptions
        {
            BucketName = "my-bucket"
        };
        var service = CreateService(options);

        using var stream = new MemoryStream([0x01, 0x02]);
        string url = await service.UploadAsync(stream, "test.webp", "image/webp");

        Assert.StartsWith("https://my-bucket.r2.cloudflarestorage.com/profiles/", url);
        Assert.EndsWith(".webp", url);
    }

    [Fact]
    public async Task DeleteAsync_WithValidCdnUrl_CallsDeleteWithExtractedKey()
    {
        var options = new BlobStorageOptions
        {
            BucketName = "my-bucket"
        };
        var service = CreateService(options);

        await service.DeleteAsync("https://cdn.example.com/profiles/avatar.png");

        _s3ClientMock.Verify(s3 => s3.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(req =>
                req.BucketName == "my-bucket" &&
                req.Key == "profiles/avatar.png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithUrlContainingBucketName_StripsBucketName()
    {
        var options = new BlobStorageOptions
        {
            BucketName = "my-bucket"
        };
        var service = CreateService(options);

        await service.DeleteAsync("https://account.r2.cloudflarestorage.com/my-bucket/profiles/avatar.png");

        _s3ClientMock.Verify(s3 => s3.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(req =>
                req.BucketName == "my-bucket" &&
                req.Key == "profiles/avatar.png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-absolute-url")]
    public async Task DeleteAsync_WithInvalidUrl_DoesNotCallS3(string invalidUrl)
    {
        var options = new BlobStorageOptions { BucketName = "my-bucket" };
        var service = CreateService(options);

        await service.DeleteAsync(invalidUrl);

        _s3ClientMock.Verify(s3 => s3.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenS3Throws_CatchesAndLogsWarning()
    {
        var options = new BlobStorageOptions { BucketName = "my-bucket" };
        _s3ClientMock
            .Setup(s3 => s3.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("R2 failure"));

        var service = CreateService(options);

        // Should not throw
        await service.DeleteAsync("https://cdn.example.com/profiles/avatar.png");

        _s3ClientMock.Verify(s3 => s3.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_CanResolveBlobStorageService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:BucketName"] = "test-bucket",
                ["BlobStorage:AccessKey"] = "test-access-key",
                ["BlobStorage:SecretKey"] = "test-secret-key",
                ["BlobStorage:ServiceUrl"] = "https://test.r2.cloudflarestorage.com"
            })
            .Build();

        services.AddLogging();
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            var config = new AmazonS3Config { ForcePathStyle = options.ForcePathStyle };
            if (!string.IsNullOrWhiteSpace(options.ServiceUrl)) config.ServiceURL = options.ServiceUrl;
            if (!string.IsNullOrWhiteSpace(options.Region)) config.AuthenticationRegion = options.Region;
            return new AmazonS3Client(new Amazon.Runtime.BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });
        services.AddScoped<IBlobStorageService, CloudflareR2BlobStorageService>();

        using var sp = services.BuildServiceProvider();
        var blobService = sp.GetService<IBlobStorageService>();

        Assert.NotNull(blobService);
        Assert.IsType<CloudflareR2BlobStorageService>(blobService);
    }
}
