using System.ComponentModel.DataAnnotations;
using SocialMediaAppBackend.DTOs.User;

namespace SocialMediaAppBackend.DTOs.Post;

public class PostResponseDto
{
    public int Id { get; set; }
    [Required]
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int AuthorId { get; set; }
}