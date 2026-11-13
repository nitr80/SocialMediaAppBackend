using System.Net;
using System.Net.Http.Json;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

/// <summary>
/// Confirms the auth rate limiter (configurable through
/// <c>RateLimiting:AuthPermitLimit</c>) actually rejects once the limit is hit.
/// Under the test server there is no remote IP, so all requests share a single
/// partition and the limit is deterministic.
/// </summary>
public sealed class RateLimitTests : IntegrationTestBase
{
    public RateLimitTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Login_ExceedingAuthLimit_ReturnsTooManyRequests()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        const int limit = 3;
        using ApiFactory factory = CreateFactory(authPermitLimit: limit);
        using HttpClient client = factory.CreateClient();

        LoginDto request = new() { Username = UniqueName("nobody"), Password = Password };

        for (int i = 0; i < limit; i++)
        {
            HttpResponseMessage allowed = await client.PostAsJsonAsync("/Auth/Login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        HttpResponseMessage blocked = await client.PostAsJsonAsync("/Auth/Login", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }
}
