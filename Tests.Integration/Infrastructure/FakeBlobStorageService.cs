using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Tests.Integration.Infrastructure;

/// <summary>
/// In-memory stand-in for the Cloudflare R2 blob service so integration tests
/// never make outbound S3 calls.
/// </summary>
public sealed class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
        => Task.FromResult($"/images/profiles/{fileName}");

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => Task.CompletedTask;
}
