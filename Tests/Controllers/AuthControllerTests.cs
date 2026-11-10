using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();

    private static AuthResponseDto SampleResponse()
    {
        return new AuthResponseDto
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = new UserResponseDto
            {
                Id = 1,
                Username = "alice",
                Email = "alice@example.com"
            }
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

    // ---------- LoginAsync ----------

    [Fact]
    public async Task LoginAsync_WhenServiceSucceeds_ReturnsOkWithData()
    {
        var response = SampleResponse();
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Ok(response));

        var controller = new AuthController(_authService.Object);

        var result = await controller.LoginAsync(new LoginDto { Username = "alice", Password = "x" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task LoginAsync_WhenServiceFails_ReturnsUnauthorizedWithMessage()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Fail("Invalid credentials", ErrorKind.Unauthorized));

        var controller = new AuthController(_authService.Object);

        var result = await controller.LoginAsync(new LoginDto { Username = "alice", Password = "x" });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        AssertMessageResult(objectResult, StatusCodes.Status401Unauthorized, "Invalid credentials");
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_WhenServiceSucceeds_ReturnsOkWithData()
    {
        var response = SampleResponse();
        _authService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Ok(response));

        var controller = new AuthController(_authService.Object);

        var result = await controller.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "Secret123"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task RegisterAsync_WhenServiceConflicts_ReturnsConflictWithMessage()
    {
        _authService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Fail("User with this email or username already exists", ErrorKind.Conflict));

        var controller = new AuthController(_authService.Object);

        var result = await controller.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "Secret123"
        });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        AssertMessageResult(objectResult, StatusCodes.Status409Conflict, "User with this email or username already exists");
    }

    [Fact]
    public void RegisterDto_WithValidPassword_PassesValidation()
    {
        var dto = new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "ValidPassword1!"
        };

        var errors = ValidateModel(dto);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("short1!", "Password must be at least 8 characters long")]
    [InlineData("nouppercase1!", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    [InlineData("NOLOWERCASE1!", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    [InlineData("NoDigitsHere!", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    [InlineData("NoSpecialCharacter1", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public void RegisterDto_WithInvalidPassword_FailsValidationWithExpectedMessage(string password, string expectedError)
    {
        var dto = new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = password
        };

        var errors = ValidateModel(dto);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage == expectedError);
    }

    [Fact]
    public void RegisterDto_WhenPasswordExceeds72Characters_FailsValidation()
    {
        var dto = new RegisterDto
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = new string('A', 70) + "a1!" // length 73
        };

        var errors = ValidateModel(dto);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage == "Password cannot exceed 72 characters");
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    // ---------- Refresh ----------

    [Fact]
    public async Task Refresh_WhenServiceSucceeds_ReturnsOkWithData()
    {
        var response = SampleResponse();
        _authService
            .Setup(s => s.Refresh(It.IsAny<TokenRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Ok(response));

        var controller = new AuthController(_authService.Object);

        var result = await controller.Refresh(new TokenRequestDto { RefreshToken = "refresh-token" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Refresh_WhenServiceFails_ReturnsUnauthorizedWithMessage()
    {
        _authService
            .Setup(s => s.Refresh(It.IsAny<TokenRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Fail("Invalid refresh token", ErrorKind.Unauthorized));

        var controller = new AuthController(_authService.Object);

        var result = await controller.Refresh(new TokenRequestDto { RefreshToken = "bad" });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        AssertMessageResult(objectResult, StatusCodes.Status401Unauthorized, "Invalid refresh token");
    }

    // ---------- LogoutAsync ----------

    [Fact]
    public async Task LogoutAsync_WhenServiceSucceeds_ReturnsNoContent()
    {
        _authService
            .Setup(s => s.LogoutAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new AuthController(_authService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LogoutAsync();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task LogoutAsync_WhenServiceFails_ReturnsUnauthorizedWithMessage()
    {
        _authService
            .Setup(s => s.LogoutAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail("User not found", ErrorKind.Unauthorized));

        var controller = new AuthController(_authService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.LogoutAsync();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status401Unauthorized, "User not found");
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
}
