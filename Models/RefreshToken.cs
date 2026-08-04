namespace SocialMediaAppBackend.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public bool IsUsed { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}