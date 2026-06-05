using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAppBackend.Models;

public class CommentLike
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public int CommentId { get; set; }
    [ForeignKey(nameof(CommentId))]
    public Comment Comment { get; set; } = null!;

    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}