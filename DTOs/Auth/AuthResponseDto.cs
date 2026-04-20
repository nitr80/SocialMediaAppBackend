using System.ComponentModel.DataAnnotations;
using SocialMediaAppBackend.DTOs.User;

namespace SocialMediaAppBackend.DTOs.Auth;

public class AuthResponseDto
{
    [Required]
    public required string Token { get; set; }
    [Required]
    public required UserResponseDto User { get; set; }
}