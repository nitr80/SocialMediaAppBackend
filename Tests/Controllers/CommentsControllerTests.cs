using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ICommentsService> _commentsService = new();

    private static Comment SampleComment()
    {
        return new Comment
        {
            Id = 1,
            Content = "nice post",
            ParentPostId = 7,
            AuthorId = 42,
            Author = new User
            {
                Id = 42,
                Username = "alice",
                Email = "alice@example.com",
                PasswordHash = "hash"
            }
        };
    }

    private static ControllerContext BuildControllerContextWithUserId(int userId)
    {
        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        userId.ToString())
                },
                "TestAuth"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    // ---------- CreateNested ----------

    [Fact]
    public async Task CreateNested_WhenServiceSucceeds_ReturnsCreatedAtAction()
    {
        _commentsService
            .Setup(s => s.CreateComment(It.IsAny<Comment>()))
            .ReturnsAsync(Result<Comment>.Ok(SampleComment()));

        var controller = new CommentsController(_commentsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.CreateNested(7, new CommentRequestDto { Content = "nice post" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CommentsController.GetAllByPostIdNested), created.ActionName);

        var dto = Assert.IsType<CommentResponseDto>(created.Value);
        Assert.Equal("nice post", dto.Content);
        Assert.Equal(7, dto.PostId);

        // comment's post id and author id came from the route and the user claim
        _commentsService.Verify(
            s => s.CreateComment(It.Is<Comment>(c => c.ParentPostId == 7 && c.AuthorId == 42)),
            Times.Once);
    }

    // ---------- GetAllByPostIdNested ----------

    [Fact]
    public async Task GetAllByPostIdNested_ReturnsOkWithComments()
    {
        var comments = new List<CommentResponseDto>
        {
            new()
            {
                Id = 1,
                Content = "nice post",
                PostId = 7,
                Author = new UserResponseDto { Id = 42, Username = "alice", Email = "alice@example.com" }
            }
        };

        _commentsService
            .Setup(s => s.GetCommentsByPostId(7))
            .ReturnsAsync(Result<List<CommentResponseDto>>.Ok(comments));

        var controller = new CommentsController(_commentsService.Object);

        var result = await controller.GetAllByPostIdNested(7);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(comments, ok.Value);
    }
}
