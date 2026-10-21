using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class CommentLikesControllerTests
{
    private readonly Mock<ICommentLikesService> _commentLikesService = new();

    private static void AssertMessageResult(ObjectResult result, int statusCode, string message)
    {
        Assert.Equal(statusCode, result.StatusCode);

        var payload = result.Value;
        Assert.NotNull(payload);
        var prop = payload!.GetType().GetProperty("message");
        Assert.NotNull(prop);
        Assert.Equal(message, prop!.GetValue(payload));
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

    // ---------- Get ----------

    [Fact]
    public async Task Get_ReturnsLikeStateForCurrentUser()
    {
        _commentLikesService
            .Setup(s => s.GetLike(5, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new CommentLikesController(_commentLikesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.Get(5);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(true, ok.Value);
    }

    // ---------- GetAllByPostId ----------

    [Fact]
    public async Task GetAllByPostId_ReturnsLikedCommentIdsForPost()
    {
        var ids = new List<int> { 10, 11 };
        _commentLikesService
            .Setup(s => s.GetAllLikedCommentIdsByUserIdAndPostId(42, 7))
            .ReturnsAsync(Result<List<int>>.Ok(ids));

        var controller = new CommentLikesController(_commentLikesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetAllByPostId(7);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(ids, ok.Value);
    }

    // ---------- LikeResource ----------

    [Fact]
    public async Task LikeResource_WhenServiceSucceeds_ReturnsOkWithData()
    {
        _commentLikesService
            .Setup(s => s.LikeComment(5, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new CommentLikesController(_commentLikesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LikeResource(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task LikeResource_WhenAlreadyLiked_ReturnsConflictWithMessage()
    {
        _commentLikesService
            .Setup(s => s.LikeComment(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Fail("Comment already liked by the user", ErrorKind.Conflict));

        var controller = new CommentLikesController(_commentLikesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LikeResource(5);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status409Conflict, "Comment already liked by the user");
    }

    // ---------- UnlikeResource ----------

    [Fact]
    public async Task UnlikeResource_WhenServiceSucceeds_ReturnsNoContent()
    {
        _commentLikesService
            .Setup(s => s.UnlikeComment(5, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new CommentLikesController(_commentLikesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.UnlikeResource(5);

        Assert.IsType<NoContentResult>(result);
    }
}
