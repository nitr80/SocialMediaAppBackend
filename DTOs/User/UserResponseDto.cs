namespace SocialMediaAppBackend.DTOs.User;

public class UserResponseDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }

}