using System.IdentityModel.Tokens.Jwt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class AuthServiceTests
{
    private const string JwtKey = "f8sLp3QzC1vB7nR2wT6yU9aD4gH1jK5mN8pX0zQ3sV6bW9cE2fG5hJ7kL0mP4nR1";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";

    private static AppDbContext CreateDbContext()
    {
        // SQLite in-memory needs the connection kept open for the DB's lifetime,
        // and it supports ExecuteDelete/ExecuteUpdate which the InMemory provider does not.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static AuthService CreateService(AppDbContext db)
    {
        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Key = JwtKey,
            Issuer = JwtIssuer,
            Audience = JwtAudience
        });

        return new AuthService(db, jwtOptions);
    }

    private static User CreateUser(string username = "alice", string password = "Secret123")
    {
        return new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_WithNewUser_ReturnsTokensAndPersistsUser()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterDto
        {
            Username = "NewUser",
            Email = "NewUser@Example.com",
            Password = "Secret123"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));

        // username and email are normalised to lowercase
        var stored = await db.Users.SingleAsync();
        Assert.Equal("newuser", stored.Username);
        Assert.Equal("newuser@example.com", stored.Email);

        // refresh token is stored hashed, never in plaintext
        var token = await db.RefreshTokens.SingleAsync();
        Assert.NotEqual(result.Data.RefreshToken, token.Token);
        Assert.Equal(64, token.Token.Length);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsernameOrEmail_ReturnsConflict()
    {
        using var db = CreateDbContext();
        db.Users.Add(CreateUser("existing"));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterDto
        {
            Username = "existing",
            Email = "another@example.com",
            Password = "Secret123"
        });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
    }

    // ---------- LoginAsync ----------

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        using var db = CreateDbContext();
        db.Users.Add(CreateUser("alice", "Secret123"));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "ALICE",
            Password = "Secret123"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        using var db = CreateDbContext();
        db.Users.Add(CreateUser("alice", "Secret123"));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "alice",
            Password = "WrongPassword"
        });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ReturnsUnauthorized()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "ghost",
            Password = "Secret123"
        });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    // ---------- Refresh ----------

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokensAndMarksOldUsed()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var register = await service.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "Secret123"
        });
        string refreshToken = register.Data!.RefreshToken;

        var result = await service.Refresh(new TokenRequestDto { RefreshToken = refreshToken });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEqual(refreshToken, result.Data!.RefreshToken);

        // one used (old) + one active (new)
        var tokens = await db.RefreshTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Contains(tokens, t => t.IsUsed);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.Refresh(new TokenRequestDto { RefreshToken = "not-a-real-token" });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var raw = "expired-token-value";
        db.RefreshTokens.Add(new RefreshToken
        {
            Token = Hash(raw),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.Refresh(new TokenRequestDto { RefreshToken = raw });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task Refresh_WhenTokenAlreadyUsed_RevokesAllActiveTokens()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var reused = "reused-token-value";
        var other = "other-active-token";

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = Hash(reused),
            UserId = user.Id,
            IsUsed = true,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            Token = Hash(other),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.Refresh(new TokenRequestDto { RefreshToken = reused });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);

        // reuse detection revoked every remaining active token for the user
        var active = await db.RefreshTokens.Where(t => t.IsRevoked == false).ToListAsync();
        Assert.Empty(active);
    }

    // ---------- LogoutAsync ----------

    [Fact]
    public async Task LogoutAsync_RevokesAllActiveTokens()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var register = await service.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "Secret123"
        });

        var user = await db.Users.SingleAsync();
        var result = await service.LogoutAsync(user.Id);

        Assert.True(result.Success);

        var tokens = await db.RefreshTokens.ToListAsync();
        Assert.All(tokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownUser_ReturnsUnauthorized()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.LogoutAsync(999);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Unauthorized, result.Kind);
    }

    // ---------- JWT content ----------

    [Fact]
    public async Task LoginAsync_AccessToken_ContainsUserIdAndUsernameClaims()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice", "Secret123");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "alice",
            Password = "Secret123"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Data!.AccessToken);

        Assert.Equal(JwtIssuer, jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == JwtAudience);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "alice");
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token)));
    }
}
