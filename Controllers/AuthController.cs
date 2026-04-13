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
    public async Task<IActionResult> LoginAsync(LoginDto loginDto)
    {
        AuthResult authResult = await _authService.LoginAsync(loginDto);

        if (!authResult.Success)
        {
            return BadRequest(authResult.Error);
        }
        
        return Ok(authResult.Data);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterDto registerDto)
    {
        AuthResult authResult = await _authService.RegisterAsync(registerDto);

        if (!authResult.Success)
        {
            return BadRequest(authResult.Error);
        }
        
        return Ok(authResult.Data);
    }
}