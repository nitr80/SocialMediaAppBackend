using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Auth;

public class TokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}