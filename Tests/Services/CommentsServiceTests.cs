using Microsoft.Data.Sqlite;
using System;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Services;
using SocialMediaAppBackend.Results;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class CommentsServiceTests
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

    // ---------- CreateComment ----------

    [Fact]
    public async Task CreateComment_WhenPostExists_PersistsCommentWithAuthorLoaded()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new CommentsService(db);

        var result = await service.CreateComment(new Comment
        {
            Content = "nice post",
            ParentPostId = post.Id,
            AuthorId = author.Id
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEqual(0, result.Data!.Id);
        Assert.Equal("nice post", result.Data.Content);

        // Author is eagerly loaded so the mapping does not hit a null reference
        Assert.NotNull(result.Data.Author);
        Assert.Equal("alice", result.Data.Author.Username);
    }

    [Fact]
    public async Task CreateComment_WhenNoPost_ReturnsNotFoundError()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var service = new CommentsService(db);

        var result = await service.CreateComment(new Comment
        {
            Content = "no post",
            ParentPostId = 7,
            AuthorId = author.Id
        });

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    // ---------- GetCommentsByPostId ----------

    [Fact]
    public async Task GetCommentsByPostId_WhenPostExists_ReturnsCommentsWithCountsAndAuthorOrderedNewestFirst()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var older = new Comment
        {
            Content = "older",
            ParentPostId = post.Id,
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Comment
        {
            Content = "newer",
            ParentPostId = post.Id,
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Comments.AddRange(older, newer);
        await db.SaveChangesAsync();

        db.CommentLikes.Add(new CommentLike { CommentId = newer.Id, UserId = author.Id });
        await db.SaveChangesAsync();

        var service = new CommentsService(db);

        var result = await service.GetCommentsByPostId(post.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);

        // newest first
        Assert.Equal("newer", result.Data[0].Content);
        Assert.Equal("older", result.Data[1].Content);

        Assert.Equal(post.Id, result.Data[0].PostId);
        Assert.Equal(1, result.Data[0].LikeCount);
        Assert.Equal("alice", result.Data[0].Author.Username);
        Assert.Equal(0, result.Data[1].LikeCount);
    }

    [Fact]
    public async Task GetCommentsByPostId_NoPost_ReturnsNotFoundError()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post to be deleted", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var older = new Comment
        {
            Content = "older",
            ParentPostId = post.Id,
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Comment
        {
            Content = "newer",
            ParentPostId = post.Id,
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Comments.AddRange(older, newer);
        await db.SaveChangesAsync();

        db.CommentLikes.Add(new CommentLike { CommentId = newer.Id, UserId = author.Id });
        await db.SaveChangesAsync();

        int postId = post.Id;

        await db.Posts.ExecuteDeleteAsync();

        var service = new CommentsService(db);

        var result = await service.GetCommentsByPostId(postId);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetCommentsByPostId_WhenNoComments_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = author.Id };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new CommentsService(db);

        var result = await service.GetCommentsByPostId(post.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetCommentsByPostId_OnlyReturnsCommentsForThatPost()
    {
        using var db = CreateDbContext();
        var author = CreateUser("alice");
        db.Users.Add(author);
        await db.SaveChangesAsync();

        var postA = new Post { Content = "post A", AuthorId = author.Id };
        var postB = new Post { Content = "post B", AuthorId = author.Id };
        db.Posts.AddRange(postA, postB);
        await db.SaveChangesAsync();

        db.Comments.AddRange(
            new Comment { Content = "on A", ParentPostId = postA.Id, AuthorId = author.Id },
            new Comment { Content = "on B", ParentPostId = postB.Id, AuthorId = author.Id });
        await db.SaveChangesAsync();

        var service = new CommentsService(db);

        var result = await service.GetCommentsByPostId(postA.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("on A", result.Data[0].Content);
    }
}
