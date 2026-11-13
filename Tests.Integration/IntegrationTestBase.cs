using System.Net.Http.Headers;
using System.Net.Http.Json;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase
{
    protected const string Password = "Secret123!";

    protected IntegrationTestBase(PostgresFixture fixture) => Fixture = fixture;

    protected PostgresFixture Fixture { get; }

    /// <summary>
    /// Skips (rather than fails) when integration tests are disabled, then returns
    /// a factory wired to the shared container.
    /// </summary>
    protected ApiFactory CreateFactory(int authPermitLimit = 1_000_000)
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);
        return new ApiFactory(Fixture.ConnectionString, authPermitLimit);
    }

    // Alphanumeric only, so it never trips ILIKE wildcard characters ('%', '_').
    protected static string UniqueName(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..20];

    protected static async Task<AuthResponseDto> RegisterAsync(HttpClient client, string username)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/Auth/Register", new RegisterDto
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = Password
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    protected static void Authenticate(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
