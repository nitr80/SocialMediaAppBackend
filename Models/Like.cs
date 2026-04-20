using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAppBackend.Models;

public class Like
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public int PostId { get; set; }
    [ForeignKey(nameof(PostId))]
    public Post Post { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}