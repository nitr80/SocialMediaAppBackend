using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUsersService> _usersService = new();

    private static User SampleUser()
    {
        return new User
        {
            Id = 1,
            Username = "alice",
            Email = "alice@example.com",
            Bio = "hello",
            PasswordHash = "hash"
        };
    }

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

    // ---------- GetById ----------

    [Fact]
    public async Task GetById_WhenUserExists_ReturnsOkWithMappedDto()
    {
        _usersService
            .Setup(s => s.GetUserById(1))
            .ReturnsAsync(Result<User>.Ok(SampleUser()));

        var controller = new UsersController(_usersService.Object);

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserResponseDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal("alice", dto.Username);
        Assert.Equal("alice@example.com", dto.Email);
    }

    [Fact]
    public async Task GetById_WhenUserMissing_ReturnsNotFoundWithMessage()
    {
        _usersService
            .Setup(s => s.GetUserById(999))
            .ReturnsAsync(Result<User>.Fail("User not found", ErrorKind.NotFound));

        var controller = new UsersController(_usersService.Object);

        var result = await controller.GetById(999);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status404NotFound, "User not found");
    }

    // ---------- AddOrUpdateBio ----------

    [Fact]
    public async Task AddOrUpdateBio_WhenServiceSucceeds_ReturnsOkWithData()
    {
        _usersService
            .Setup(s => s.AddOrUpdateBio("new bio", 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.AddOrUpdateBio(new UserBioRequestDto { Bio = "new bio" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task AddOrUpdateBio_WhenUserMissing_ReturnsNotFoundWithMessage()
    {
        _usersService
            .Setup(s => s.AddOrUpdateBio(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Fail("User not found", ErrorKind.NotFound));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.AddOrUpdateBio(new UserBioRequestDto { Bio = "new bio" });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status404NotFound, "User not found");
    }

    // ---------- AddOrUpdateProfilePicture ----------

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenServiceSucceeds_ReturnsOkWithData()
    {
        _usersService
            .Setup(s => s.AddOrUpdateProfilePicture(It.IsAny<IFormFile>(), 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var file = new FormFile(new MemoryStream(new byte[4]), 0, 4, "image", "avatar.png");

        var result = await controller.AddOrUpdateProfilePicture(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenValidationFails_ReturnsBadRequestWithMessage()
    {
        _usersService
            .Setup(s => s.AddOrUpdateProfilePicture(It.IsAny<IFormFile>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Fail("Only jpg, jpeg, png and webp images are allowed", ErrorKind.Validation));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var file = new FormFile(new MemoryStream(new byte[4]), 0, 4, "image", "avatar.gif");

        var result = await controller.AddOrUpdateProfilePicture(file);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status400BadRequest, "Only jpg, jpeg, png and webp images are allowed");
    }

    // --------- SearchUsername ----------

    [Fact]
    public async Task SearchUsername_WhenSearchTermProvided_ReturnsMatchingUsers()
    {
        _usersService
            .Setup(s => s.SearchUsername("john", 42))
            .ReturnsAsync(Result<List<UserResponseDto>>.Ok(new List<UserResponseDto>()));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.SearchUsername("john");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchUsername_WhenNoSearchTermProvided_ReturnsAllUsers()
    {
        _usersService
            .Setup(s => s.SearchUsername(null, 42))
            .ReturnsAsync(Result<List<UserResponseDto>>.Ok(new List<UserResponseDto>()));

        var controller = new UsersController(_usersService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.SearchUsername(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
    }
}
