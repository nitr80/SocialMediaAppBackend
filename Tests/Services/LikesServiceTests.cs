using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class LikesServiceTests
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

    // ---------- LikePost ----------

    [Fact]
    public async Task LikePost_AddsLike()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = user.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new LikesService(db);

        var result = await service.LikePost(post.Id, user.Id);

        Assert.True(result.Success);
        Assert.True(await db.Likes.AnyAsync(l => l.PostId == post.Id && l.UserId == user.Id));
    }

    [Fact]
    public async Task LikePost_WhenAlreadyLiked_ReturnsConflict()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = user.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new LikesService(db);
        await service.LikePost(post.Id, user.Id);

        var result = await service.LikePost(post.Id, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal(1, await db.Likes.CountAsync());
    }

    // ---------- GetLike ----------

    [Fact]
    public async Task GetLike_ReturnsTrueWhenLikedAndFalseWhenNot()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = user.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new LikesService(db);

        var before = await service.GetLike(post.Id, user.Id);
        Assert.True(before.Success);
        Assert.False(before.Data);

        await service.LikePost(post.Id, user.Id);

        var after = await service.GetLike(post.Id, user.Id);
        Assert.True(after.Success);
        Assert.True(after.Data);
    }

    // ---------- GetAllLikedPostIdsByUserId ----------

    [Fact]
    public async Task GetAllLikedPostIdsByUserId_ReturnsOnlyThatUsersLikes()
    {
        using var db = CreateDbContext();
        var alice = CreateUser("alice");
        var bob = CreateUser("bob");
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var post1 = new Post { Content = "one", AuthorId = alice.Id };
        var post2 = new Post { Content = "two", AuthorId = alice.Id };
        db.Posts.AddRange(post1, post2);
        await db.SaveChangesAsync();

        db.Likes.AddRange(
            new Like { PostId = post1.Id, UserId = alice.Id },
            new Like { PostId = post2.Id, UserId = bob.Id });
        await db.SaveChangesAsync();

        var service = new LikesService(db);

        var result = await service.GetAllLikedPostIdsByUserId(alice.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal(post1.Id, result.Data[0]);
    }

    // ---------- UnlikePost ----------

    [Fact]
    public async Task UnlikePost_RemovesExistingLike()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = user.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new LikesService(db);
        await service.LikePost(post.Id, user.Id);

        var result = await service.UnlikePost(post.Id, user.Id);

        Assert.True(result.Success);
        Assert.False(await db.Likes.AnyAsync());
    }

    [Fact]
    public async Task UnlikePost_WhenNotLiked_IsIdempotentSuccess()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = user.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new LikesService(db);

        var result = await service.UnlikePost(post.Id, user.Id);

        Assert.True(result.Success);
        Assert.True(result.Data);
    }
}
