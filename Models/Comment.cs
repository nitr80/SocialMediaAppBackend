using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAppBackend.Models;

public class Comment
{
    public int Id { get; set; }
    [Required]
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // public int LikeCount { get; set; } = 0;

    public int AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))]
    public User Author { get; set; } = null!;

    public int ParentPostId { get; set; }
    [ForeignKey(nameof(ParentPostId))]
    public Post ParentPost { get; set; } = null!;

    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

}