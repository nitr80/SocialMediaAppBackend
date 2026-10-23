using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAppBackend.Models;

public class Follow
{
    public int Id { get; set; }

    public int FollowerId { get; set; }
    [ForeignKey(nameof(FollowerId))]
    public User Follower { get; set; } = null!;

    public int FollowedUserId { get; set; }
    [ForeignKey(nameof(FollowedUserId))]
    public User FollowedUser { get; set; } = null!;

    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}