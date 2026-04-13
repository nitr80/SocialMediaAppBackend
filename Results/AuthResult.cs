using SocialMediaAppBackend.DTOs.Auth;

namespace SocialMediaAppBackend.Results;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public AuthResponseDto? Data { get; set; }
}