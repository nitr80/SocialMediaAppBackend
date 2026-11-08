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
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [MaxLength(72, ErrorMessage = "Password cannot exceed 72 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()-+]).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public required string Password { get; set; }
}