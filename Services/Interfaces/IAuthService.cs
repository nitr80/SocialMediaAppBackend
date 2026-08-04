using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<Result<AuthResponseDto>> Refresh(TokenRequestDto tokenRequestDto);
    Task<Result<bool>> LogoutAsync(int userId);
}