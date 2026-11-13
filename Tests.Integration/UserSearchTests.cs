using System.Net.Http.Json;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

/// <summary>
/// Exercises the case-insensitive username search, which on PostgreSQL runs
/// through <c>EF.Functions.ILike</c> and the pg_trgm GIN index (the SQLite path
/// in the unit tests takes a completely different branch).
/// </summary>
public sealed class UserSearchTests : IntegrationTestBase
{
    public UserSearchTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Search_IsCaseInsensitiveAndExcludesCurrentUser()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient alice = factory.CreateClient();
        using HttpClient bob = factory.CreateClient();

        string aliceName = UniqueName("alice");
        string bobName = UniqueName("bob");

        await RegisterAsync(alice, aliceName);
        AuthResponseDto bobAuth = await RegisterAsync(bob, bobName);
        Authenticate(bob, bobAuth.AccessToken);

        // Uppercase query must still match the lowercase stored username.
        List<UserPublicDto>? matches =
            await bob.GetFromJsonAsync<List<UserPublicDto>>($"/Users?search={aliceName.ToUpperInvariant()}");

        Assert.NotNull(matches);
        Assert.Contains(matches!, u => u.Username == aliceName);

        // Searching for your own name excludes yourself.
        List<UserPublicDto>? selfMatches =
            await bob.GetFromJsonAsync<List<UserPublicDto>>($"/Users?search={bobName}");

        Assert.NotNull(selfMatches);
        Assert.DoesNotContain(selfMatches!, u => u.Username == bobName);
    }
}
