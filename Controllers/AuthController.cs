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
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        Result<AuthResponseDto> authResult = await _authService.LoginAsync(loginDto);

        return authResult.ToActionResult(this);
    }

    [HttpPost("Register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        Result<AuthResponseDto> authResult = await _authService.RegisterAsync(registerDto);

        return authResult.ToActionResult(this);
    }

    [HttpPost("Refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(TokenRequestDto requestDto)
    {
        Result<AuthResponseDto> authResult = await _authService.Refresh(requestDto);

        return authResult.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<ActionResult> LogoutAsync()
    {
        Result<bool> result = await _authService.LogoutAsync(User.GetUserId());

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }
}