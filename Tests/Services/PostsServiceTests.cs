using Microsoft.Data.Sqlite;
using System;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class PostsServiceTests
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

    // ---------- CreatePost ----------

    [Fact]
    public async Task CreatePost_PersistsPostWithAuthorLoaded()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.CreatePost(new Post
        {
            Content = "Hello world",
            AuthorId = author.Id
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEqual(0, result.Data!.Id);
        Assert.Equal("Hello world", result.Data.Content);

        // Author is eagerly loaded so the mapping does not hit a null reference
        Assert.NotNull(result.Data.Author);
        Assert.Equal("alice", result.Data.Author.Username);
    }

    // ---------- GetPostById ----------

    [Fact]
    public async Task GetPostById_WhenOwner_ReturnsPostWithAuthor()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetPostById(post.Id, author.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(post.Id, result.Data!.Id);
        Assert.NotNull(result.Data.Author);
    }

    [Fact]
    public async Task GetPostById_WhenFollowing_ReturnsPostWithAuthor()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        var follower = CreateUser("bob");
        db.Users.AddRange(author, follower);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        db.Follows.Add(new Follow { FollowerId = follower.Id, FollowedUserId = author.Id });
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetPostById(post.Id, follower.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(post.Id, result.Data!.Id);
        Assert.NotNull(result.Data.Author);
    }

    [Fact]
    public async Task GetPostById_WhenPostMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();

        var service = new PostsService(db);
        var result = await service.GetPostById(999, 1);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetPostById_WhenNotFollowing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        var other = CreateUser("bob");
        db.Users.AddRange(author, other);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetPostById(post.Id, other.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Forbidden, result.Kind);
    }

    // ---------- GetAllPosts ----------

    [Fact]
    public async Task GetAllPosts_WhenOwner_ReturnsPostsWithCountsAndAuthorOrderedNewestFirst()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var older = new Post
        {
            Content = "older",
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Post
        {
            Content = "newer",
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.AddRange(older, newer);
        await db.SaveChangesAsync();

        db.Likes.Add(new Like { PostId = newer.Id, UserId = author.Id });
        db.Comments.Add(new Comment { ParentPostId = newer.Id, AuthorId = author.Id, Content = "nice" });
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetAllPosts(author.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);

        // newest first
        Assert.Equal("newer", result.Data[0].Content);
        Assert.Equal("older", result.Data[1].Content);

        Assert.Equal(1, result.Data[0].LikeCount);
        Assert.Equal(1, result.Data[0].CommentCount);
        Assert.Equal("alice", result.Data[0].Author.Username);
        Assert.Equal(0, result.Data[1].LikeCount);
        Assert.Equal(0, result.Data[1].CommentCount);
    }

    [Fact]
    public async Task GetAllPosts_WhenFollowing_ReturnsPostsWithCountsAndAuthorOrderedNewestFirst()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        var follower = CreateUser("bob");
        db.Users.Add(author);
        db.Users.Add(follower);
        await db.SaveChangesAsync();

        var older = new Post
        {
            Content = "older",
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Post
        {
            Content = "newer",
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.AddRange(older, newer);
        await db.SaveChangesAsync();

        db.Likes.Add(new Like { PostId = newer.Id, UserId = author.Id });
        db.Comments.Add(new Comment { ParentPostId = newer.Id, AuthorId = author.Id, Content = "nice" });
        await db.SaveChangesAsync();

        db.Follows.Add(new Follow { FollowerId = follower.Id, FollowedUserId = author.Id });
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetAllPosts(follower.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);

        // newest first
        Assert.Equal("newer", result.Data[0].Content);
        Assert.Equal("older", result.Data[1].Content);

        Assert.Equal(1, result.Data[0].LikeCount);
        Assert.Equal(1, result.Data[0].CommentCount);
        Assert.Equal("alice", result.Data[0].Author.Username);
        Assert.Equal(0, result.Data[1].LikeCount);
        Assert.Equal(0, result.Data[1].CommentCount);
    }

    [Fact]
    public async Task GetAllPosts_WhenNotFollowing_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        var other = CreateUser("bob");
        db.Users.AddRange(author, other);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.GetAllPosts(other.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }

    // ---------- DeletePostById ----------

    [Fact]
    public async Task DeletePostById_WhenOwner_DeletesPost()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.DeletePostById(post.Id, author.Id);

        Assert.True(result.Success);
        Assert.False(await db.Posts.AnyAsync(p => p.Id == post.Id));
    }

    [Fact]
    public async Task DeletePostById_WhenNotOwner_ReturnsForbiddenAndKeepsPost()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        var other = CreateUser("bob");
        db.Users.AddRange(author, other);
        await db.SaveChangesAsync();

        var post = new Post { Content = "Hello", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db);

        var result = await service.DeletePostById(post.Id, other.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Forbidden, result.Kind);
        Assert.True(await db.Posts.AnyAsync(p => p.Id == post.Id));
    }

    [Fact]
    public async Task DeletePostById_WhenPostMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = new PostsService(db);

        var result = await service.DeletePostById(999, 1);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }
}
