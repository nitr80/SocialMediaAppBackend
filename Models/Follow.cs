using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAppBackend.Models;

public class Follow
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public int FollowedUserId { get; set; }
    [ForeignKey(nameof(FollowedUserId))]
    public User FollowedUser { get; set; } = null!;

    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}