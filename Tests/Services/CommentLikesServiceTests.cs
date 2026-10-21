using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class CommentLikesServiceTests
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

    private static (Post Post, Comment Comment) SeedPostWithComment(AppDbContext db, User author)
    {
        var post = new Post { Content = "post", AuthorId = author.Id };
        db.Posts.Add(post);
        db.SaveChanges();

        var comment = new Comment
        {
            Content = "comment",
            ParentPostId = post.Id,
            AuthorId = author.Id
        };
        db.Comments.Add(comment);
        db.SaveChanges();

        return (post, comment);
    }

    // ---------- LikeComment ----------

    [Fact]
    public async Task LikeComment_AddsLike()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (_, comment) = SeedPostWithComment(db, user);

        var service = new CommentLikesService(db);

        var result = await service.LikeComment(comment.Id, user.Id);

        Assert.True(result.Success);
        Assert.True(await db.CommentLikes.AnyAsync(cl => cl.CommentId == comment.Id && cl.UserId == user.Id));
    }

    [Fact]
    public async Task LikeComment_WhenAlreadyLiked_ReturnsConflict()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (_, comment) = SeedPostWithComment(db, user);

        var service = new CommentLikesService(db);
        await service.LikeComment(comment.Id, user.Id);

        var result = await service.LikeComment(comment.Id, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal(1, await db.CommentLikes.CountAsync());
    }

    // ---------- GetLike ----------

    [Fact]
    public async Task GetLike_ReturnsTrueWhenLikedAndFalseWhenNot()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (_, comment) = SeedPostWithComment(db, user);

        var service = new CommentLikesService(db);

        var before = await service.GetLike(comment.Id, user.Id);
        Assert.True(before.Success);
        Assert.False(before.Data);

        await service.LikeComment(comment.Id, user.Id);

        var after = await service.GetLike(comment.Id, user.Id);
        Assert.True(after.Success);
        Assert.True(after.Data);
    }

    // ---------- GetAllLikedCommentIdsByUserIdAndPostId ----------

    [Fact]
    public async Task GetAllLikedCommentIdsByUserIdAndPostId_ReturnsOnlyLikedCommentsInThatPost()
    {
        using var db = CreateDbContext();
        var alice = CreateUser("alice");
        db.Users.Add(alice);
        await db.SaveChangesAsync();

        var post = new Post { Content = "post", AuthorId = alice.Id };
        var otherPost = new Post { Content = "other", AuthorId = alice.Id };
        db.Posts.AddRange(post, otherPost);
        await db.SaveChangesAsync();

        var likedComment = new Comment { Content = "liked", ParentPostId = post.Id, AuthorId = alice.Id };
        var notLikedComment = new Comment { Content = "not liked", ParentPostId = post.Id, AuthorId = alice.Id };
        var otherPostComment = new Comment { Content = "other post", ParentPostId = otherPost.Id, AuthorId = alice.Id };
        db.Comments.AddRange(likedComment, notLikedComment, otherPostComment);
        await db.SaveChangesAsync();

        db.CommentLikes.AddRange(
            new CommentLike { CommentId = likedComment.Id, UserId = alice.Id },
            new CommentLike { CommentId = otherPostComment.Id, UserId = alice.Id });
        await db.SaveChangesAsync();

        var service = new CommentLikesService(db);

        var result = await service.GetAllLikedCommentIdsByUserIdAndPostId(alice.Id, post.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal(likedComment.Id, result.Data[0]);
    }

    // ---------- UnlikeComment ----------

    [Fact]
    public async Task UnlikeComment_RemovesExistingLike()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (_, comment) = SeedPostWithComment(db, user);

        var service = new CommentLikesService(db);
        await service.LikeComment(comment.Id, user.Id);

        var result = await service.UnlikeComment(comment.Id, user.Id);

        Assert.True(result.Success);
        Assert.False(await db.CommentLikes.AnyAsync());
    }

    [Fact]
    public async Task UnlikeComment_WhenNotLiked_IsIdempotentSuccess()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (_, comment) = SeedPostWithComment(db, user);

        var service = new CommentLikesService(db);

        var result = await service.UnlikeComment(comment.Id, user.Id);

        Assert.True(result.Success);
        Assert.True(result.Data);
    }
}
