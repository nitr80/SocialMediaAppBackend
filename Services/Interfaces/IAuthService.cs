using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> Refresh(TokenRequestDto tokenRequestDto, CancellationToken cancellationToken = default);
    Task<Result<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default);
}
