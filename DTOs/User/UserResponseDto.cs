namespace SocialMediaAppBackend.DTOs.User;

using System.ComponentModel.DataAnnotations;

public class UserResponseDto
{
    public int Id { get; set; }
    [Required]
    public required string Username { get; set; }
    [Required]
    public required string Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }

}