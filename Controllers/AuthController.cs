using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        Result<AuthResponseDto> authResult = await _authService.LoginAsync(loginDto);

        if (!authResult.Success)
        {
            return Unauthorized(new { message = authResult.Error});
        }
        
        return Ok(authResult.Data);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        Result<AuthResponseDto> authResult = await _authService.RegisterAsync(registerDto);

        if (!authResult.Success)
        {
            return Conflict(new { message = authResult.Error});
        }
        
        return Ok(authResult.Data);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(TokenRequestDto requestDto)
    {
        Result<AuthResponseDto> authResult = await _authService.Refresh(requestDto);

        if (!authResult.Success)
        {
            return Unauthorized(new { message = authResult.Error});
        }

        return Ok(authResult.Data);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> LogoutAsync()
    {
        Result<bool> result = await _authService.LogoutAsync(User.GetUserId());

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Error});
        }

        return Ok();
    }
}