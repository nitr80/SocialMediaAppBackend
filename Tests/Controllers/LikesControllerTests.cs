using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class LikesControllerTests
{
    private readonly Mock<ILikesService> _likesService = new();

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
        _likesService
            .Setup(s => s.GetLike(7, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new LikesController(_likesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.Get(7);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(true, ok.Value);
    }

    // ---------- GetAll ----------

    [Fact]
    public async Task GetAll_ReturnsLikedPostIds()
    {
        var ids = new List<int> { 1, 2, 3 };
        _likesService
            .Setup(s => s.GetAllLikedPostIdsByUserId(42))
            .ReturnsAsync(Result<List<int>>.Ok(ids));

        var controller = new LikesController(_likesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(ids, ok.Value);
    }

    // ---------- LikePostResource ----------

    [Fact]
    public async Task LikePostResource_WhenServiceSucceeds_ReturnsOkWithData()
    {
        _likesService
            .Setup(s => s.LikePost(7, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new LikesController(_likesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LikePostResource(7);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task LikePostResource_WhenAlreadyLiked_ReturnsConflictWithMessage()
    {
        _likesService
            .Setup(s => s.LikePost(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Fail("Post already liked by the user", ErrorKind.Conflict));

        var controller = new LikesController(_likesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LikePostResource(7);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status409Conflict, "Post already liked by the user");
    }

    // ---------- UnlikePostResource ----------

    [Fact]
    public async Task UnlikePostResource_WhenServiceSucceeds_ReturnsNoContent()
    {
        _likesService
            .Setup(s => s.UnlikePost(7, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new LikesController(_likesService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.UnlikePostResource(7);

        Assert.IsType<NoContentResult>(result);
    }
}
