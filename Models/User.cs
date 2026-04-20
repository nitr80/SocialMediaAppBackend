using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.Models;

public class User
{
    public int Id { get; set; }
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();

}

