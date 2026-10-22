using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class FollowsServiceTests
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

    private static User CreateUser(string username)
    {
        return new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hash"
        };
    }  

    [Fact]
    public async Task FollowUser_AddsFollow()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.FollowUser(user1.Id, user2.Id);

        Assert.True(result.Success);
        Assert.True(await db.Follows.AnyAsync(f => f.UserId == user1.Id && f.FollowedUserId == user2.Id));
    }

    [Fact]
    public async Task FollowUser_WhenUserDoesNotExist_ReturnsError()
    {
        using var db = CreateDbContext();
        var service = new FollowsService(db);
        var result = await service.FollowUser(1, 2);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task FollowUser_WhenAlreadyFollowing_ReturnsError()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        await db.Follows.AddAsync(new Follow { UserId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.FollowUser(user1.Id, user2.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal(1, await db.Follows.CountAsync());
    }


    // --------- UnfollowUser ----------

    [Fact]
    public async Task UnfollowUser_RemovesFollow()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        await db.Follows.AddAsync(new Follow { UserId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();
        
        var service = new FollowsService(db);
        var result = await service.UnfollowUser(user1.Id, user2.Id);

        Assert.True(result.Success);
        Assert.False(await db.Follows.AnyAsync(f => f.UserId == user1.Id && f.FollowedUserId == user2.Id));
    }

    [Fact]
    public async Task UnfollowUser_WhenNotFollowing_ReturnsSuccess()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.UnfollowUser(user1.Id, user2.Id);
        Assert.True(result.Success);
    }


    [Fact]
    public async Task UnfollowUser_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        using var db = CreateDbContext();
        var service = new FollowsService(db);
        var result = await service.UnfollowUser(1, 2);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    // --------- GetFollowingStatus ----------

    [Fact]
    public async Task GetFollowingStatus_ReturnsTrueWhenFollowing()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        await db.Follows.AddAsync(new Follow { UserId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.GetFollowingStatus(user1.Id, user2.Id);

        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task GetFollowingStatus_ReturnsFalseWhenNotFollowing()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.GetFollowingStatus(user1.Id, user2.Id);

        Assert.True(result.Success);
        Assert.False(result.Data);
    }

    [Fact]
    public async Task GetFollowingStatus_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        using var db = CreateDbContext();
        var service = new FollowsService(db);
        var result = await service.GetFollowingStatus(1, 2);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

}