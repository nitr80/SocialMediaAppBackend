using Microsoft.AspNetCore.Authorization;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        Result<AuthResponseDto> authResult = await _authService.LoginAsync(loginDto, cancellationToken);

        return authResult.ToActionResult(this);
    }

    [HttpPost("Register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        Result<AuthResponseDto> authResult = await _authService.RegisterAsync(registerDto, cancellationToken);

        return authResult.ToActionResult(this);
    }

    [HttpPost("Refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(TokenRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        Result<AuthResponseDto> authResult = await _authService.Refresh(requestDto, cancellationToken);

        return authResult.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<ActionResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _authService.LogoutAsync(User.GetUserId(), cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }
}