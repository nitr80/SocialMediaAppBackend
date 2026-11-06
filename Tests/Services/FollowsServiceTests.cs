using Microsoft.Data.Sqlite;
using System;
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
        var result = await service.FollowUser(user2.Id, user1.Id);

        Assert.True(result.Success);
        Assert.True(await db.Follows.AnyAsync(f => f.FollowerId == user1.Id && f.FollowedUserId == user2.Id));
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
    public async Task FollowUser_WhenSelfFollowing_ReturnsValidationError()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.FollowUser(user1.Id, user1.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Equal("Users cannot follow themselves", result.Error);
        Assert.False(await db.Follows.AnyAsync());
    }

    [Fact]
    public async Task FollowUser_WhenAlreadyFollowing_ReturnsError()
    {
        using var db = CreateDbContext();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        await db.Follows.AddAsync(new Follow { FollowerId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.FollowUser(user2.Id, user1.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal(1, await db.Follows.CountAsync());
    }

    private class ThrowingDbContext : AppDbContext
    {
        public ThrowingDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Duplicate key violation", (Exception?)null);
        }
    }

    [Fact]
    public async Task FollowUser_WhenDbUpdateExceptionOccurs_ReturnsConflict()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        using var throwingDb = new ThrowingDbContext(options);
        var service = new FollowsService(throwingDb);
        var result = await service.FollowUser(user2.Id, user1.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal("Already following this user", result.Error);
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

        await db.Follows.AddAsync(new Follow { FollowerId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.UnfollowUser(user2.Id, user1.Id);

        Assert.True(result.Success);
        Assert.False(await db.Follows.AnyAsync(f => f.FollowerId == user1.Id && f.FollowedUserId == user2.Id));
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

        await db.Follows.AddAsync(new Follow { FollowerId = user1.Id, FollowedUserId = user2.Id });
        await db.SaveChangesAsync();

        var service = new FollowsService(db);
        var result = await service.GetFollowingStatus(user2.Id, user1.Id);

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