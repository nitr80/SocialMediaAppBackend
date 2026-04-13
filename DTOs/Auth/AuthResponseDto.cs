namespace SocialMediaAppBackend.DTOs.Auth;

public class AuthResponseDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string AuthJwt { get; set; }
}