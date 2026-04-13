using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    Task<AuthResult> LoginAsync(LoginDto loginDto);
}