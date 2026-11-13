using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real application (the actual <c>Program</c> pipeline) against the
/// container provided by <see cref="PostgresFixture"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    // Must be at least 32 bytes for HMAC-SHA256 signing.
    private const string TestJwtKey = "integration-test-signing-key-that-is-long-enough-1234567890";

    public ApiFactory(string connectionString, int authPermitLimit = 1_000_000)
    {
        // Program.cs validates configuration eagerly while it runs, i.e. before
        // WebApplicationFactory's ConfigureAppConfiguration callback is applied.
        // Environment variables are read by WebApplication.CreateBuilder right
        // away, so they are the only reliable way to feed these values in.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        Environment.SetEnvironmentVariable("JwtOptions__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("JwtOptions__Issuer", "integration-issuer");
        Environment.SetEnvironmentVariable("JwtOptions__Audience", "integration-audience");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitLimit", authPermitLimit.ToString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddScoped<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}
