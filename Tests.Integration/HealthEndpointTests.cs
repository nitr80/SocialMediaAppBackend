using System.Net;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

/// <summary>
/// The readiness probe runs the real database check against PostgreSQL and the
/// blob check (which is short-circuited because the bucket name is empty).
/// </summary>
public sealed class HealthEndpointTests : IntegrationTestBase
{
    public HealthEndpointTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Live_ReturnsHealthy()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_WithDatabaseAvailable_ReturnsHealthy()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
