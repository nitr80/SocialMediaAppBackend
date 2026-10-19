using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [MaxLength(100)]
    public required string Username { get; set; }
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public required string Email { get; set; }
    [Required]
    [MaxLength(72)]
    public required string Password { get; set; }
}