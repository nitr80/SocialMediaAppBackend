using System.ComponentModel.DataAnnotations;
using SocialMediaAppBackend.DTOs.User;

namespace SocialMediaAppBackend.DTOs.Auth;

public class AuthResponseDto
{
    [Required]
    public required string AccessToken { get; set; }
    [Required]
    public required string RefreshToken { get; set; }
    [Required]
    public required UserResponseDto User { get; set; }
}