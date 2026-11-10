using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class RefreshTokenCleanupServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static User CreateUser(string username = "alice")
    {
        return new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hash"
        };
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_DeletesOnlyExpiredTokensAndReturnsCount()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.RefreshTokens.AddRange(
            new RefreshToken
            {
                Token = new string('a', 64),
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(-1)
            },
            new RefreshToken
            {
                Token = new string('b', 64),
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddHours(-1),
                IsRevoked = true
            },
            new RefreshToken
            {
                Token = new string('c', 64),
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(1)
            });
        await db.SaveChangesAsync();

        var service = new RefreshTokenCleanupService(db);

        int deletedCount = await service.DeleteExpiredTokensAsync();

        Assert.Equal(2, deletedCount);

        var remaining = await db.RefreshTokens.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(new string('c', 64), remaining[0].Token);
        Assert.True(remaining[0].Expires > DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_WhenNoExpiredTokens_ReturnsZeroAndKeepsAll()
    {
        using var db = CreateDbContext();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = new string('a', 64),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var service = new RefreshTokenCleanupService(db);

        int deletedCount = await service.DeleteExpiredTokensAsync();

        Assert.Equal(0, deletedCount);
        Assert.Single(await db.RefreshTokens.ToListAsync());
    }
}
