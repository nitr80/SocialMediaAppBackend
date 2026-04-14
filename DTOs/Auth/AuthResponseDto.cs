using SocialMediaAppBackend.DTOs.User;

namespace SocialMediaAppBackend.DTOs.Auth;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required UserResponseDto User { get; set; }
}