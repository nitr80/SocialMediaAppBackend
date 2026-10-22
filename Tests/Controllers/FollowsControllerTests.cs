using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class FollowsControllerTests
{
    private readonly Mock<IFollowsService> _followsService = new();

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

    [Fact]
    public async Task FollowUser_WhenFollowIsSuccessful_ReturnsOkResult()
    {
        _followsService
            .Setup(s => s.FollowUser(1, 2))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.FollowUser(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task FollowUser_WhenUserDoesNotExist_ReturnsNotFoundResult()
    {
        _followsService
            .Setup(s => s.FollowUser(1, 2))
            .ReturnsAsync(Result<bool>.Fail("User not found", ErrorKind.NotFound));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.FollowUser(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task FollowUser_WhenAlreadyFollowing_ReturnsConflictResult()
    {
        _followsService
            .Setup(s => s.FollowUser(1, 2))
            .ReturnsAsync(Result<bool>.Fail("Already following", ErrorKind.Conflict));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.FollowUser(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UnfollowUser_WhenUnfollowIsSuccessful_ReturnsNoContentResult()
    {
        _followsService
            .Setup(s => s.UnfollowUser(1, 2))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.UnfollowUser(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UnfollowUser_WhenUserDoesNotExist_ReturnsNotFoundResult()
    {
        _followsService
            .Setup(s => s.UnfollowUser(1, 2))
            .ReturnsAsync(Result<bool>.Fail("User not found", ErrorKind.NotFound));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.UnfollowUser(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFollowingStatus_WhenFollowing_ReturnsOkResultWithTrue()
    {
        _followsService
            .Setup(s => s.GetFollowingStatus(1, 2))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.GetFollowingStatus(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task GetFollowingStatus_WhenNotFollowing_ReturnsOkResultWithFalse()
    {
        _followsService
            .Setup(s => s.GetFollowingStatus(1, 2))
            .ReturnsAsync(Result<bool>.Ok(false));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.GetFollowingStatus(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(false, ok.Value);
    }

    [Fact]
    public async Task GetFollowingStatus_WhenUserDoesNotExist_ReturnsNotFoundResult()
    {
        _followsService
            .Setup(s => s.GetFollowingStatus(1, 2))
            .ReturnsAsync(Result<bool>.Fail("User not found", ErrorKind.NotFound));

        var controller = new FollowsController(_followsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(2)
        };

        var result = await controller.GetFollowingStatus(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

}