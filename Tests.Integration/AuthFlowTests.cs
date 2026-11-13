using System.Net;
using System.Net.Http.Json;
using SocialMediaAppBackend.DTOs;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

/// <summary>
/// End-to-end exercise of the real HTTP pipeline + PostgreSQL: register, create
/// a post, like, comment, follow and read the feed.
/// </summary>
public sealed class AuthFlowTests : IntegrationTestBase
{
    public AuthFlowTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        string username = UniqueName("login");
        await RegisterAsync(client, username);

        HttpResponseMessage response = await client.PostAsJsonAsync("/Auth/Login", new LoginDto
        {
            Username = username,
            Password = Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AuthResponseDto? auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        string username = UniqueName("dup");
        await RegisterAsync(client, username);

        HttpResponseMessage response = await client.PostAsJsonAsync("/Auth/Register", new RegisterDto
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = Password
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/Posts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_CreatePostLikeCommentFollowAndFeed_Works()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient alice = factory.CreateClient();
        using HttpClient bob = factory.CreateClient();

        AuthResponseDto aliceAuth = await RegisterAsync(alice, UniqueName("alice"));
        Authenticate(alice, aliceAuth.AccessToken);
        AuthResponseDto bobAuth = await RegisterAsync(bob, UniqueName("bob"));
        Authenticate(bob, bobAuth.AccessToken);

        // Alice creates a post.
        HttpResponseMessage createResponse = await alice.PostAsJsonAsync("/Posts", new PostRequestDto
        {
            Content = "hello from alice"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        PostResponseDto created = (await createResponse.Content.ReadFromJsonAsync<PostResponseDto>())!;

        // Bob likes and comments on it.
        HttpResponseMessage likeResponse = await bob.PostAsync($"/Posts/{created.Id}/Likes", null);
        Assert.Equal(HttpStatusCode.OK, likeResponse.StatusCode);

        HttpResponseMessage commentResponse = await bob.PostAsJsonAsync(
            $"/Posts/{created.Id}/Comments",
            new CommentRequestDto { Content = "nice one" });
        Assert.Equal(HttpStatusCode.Created, commentResponse.StatusCode);

        // Bob follows Alice.
        HttpResponseMessage followResponse = await bob.PostAsync($"/Users/{aliceAuth.User.Id}/Follows", null);
        Assert.Equal(HttpStatusCode.OK, followResponse.StatusCode);

        // Alice reads the post back and sees the aggregated counters.
        HttpResponseMessage getResponse = await alice.GetAsync($"/Posts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        PostResponseDto fetched = (await getResponse.Content.ReadFromJsonAsync<PostResponseDto>())!;
        Assert.Equal("hello from alice", fetched.Content);
        Assert.Equal(1, fetched.LikeCount);
        Assert.Equal(1, fetched.CommentCount);

        // Bob's feed contains Alice's post because he follows her.
        HttpResponseMessage feedResponse = await bob.GetAsync("/Posts");
        Assert.Equal(HttpStatusCode.OK, feedResponse.StatusCode);
        PagedList<PostResponseDto> feed =
            (await feedResponse.Content.ReadFromJsonAsync<PagedList<PostResponseDto>>())!;
        Assert.Contains(feed.Values, p => p.Id == created.Id);
    }

    [Fact]
    public async Task GetPostById_WhenNotFollowing_ReturnsForbidden()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        using ApiFactory factory = CreateFactory();
        using HttpClient alice = factory.CreateClient();
        using HttpClient carol = factory.CreateClient();

        AuthResponseDto aliceAuth = await RegisterAsync(alice, UniqueName("alice"));
        Authenticate(alice, aliceAuth.AccessToken);
        AuthResponseDto carolAuth = await RegisterAsync(carol, UniqueName("carol"));
        Authenticate(carol, carolAuth.AccessToken);

        HttpResponseMessage createResponse = await alice.PostAsJsonAsync("/Posts", new PostRequestDto
        {
            Content = "private to followers"
        });
        PostResponseDto created = (await createResponse.Content.ReadFromJsonAsync<PostResponseDto>())!;

        HttpResponseMessage response = await carol.GetAsync($"/Posts/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
