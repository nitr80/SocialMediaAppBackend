using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Auth;

public class LoginDto
{
    [Required]
    [MaxLength(100)]
    public required string Username { get; set; }
    [Required]
    [MaxLength(72)]
    public required string Password { get; set; }
}