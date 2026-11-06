using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.User;

public class UserPublicDto
{
    public int Id { get; set; }
    [Required]
    public required string Username { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}
