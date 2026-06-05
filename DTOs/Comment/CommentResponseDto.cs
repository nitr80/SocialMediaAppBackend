namespace SocialMediaAppBackend.DTOs.Comment;

using System.ComponentModel.DataAnnotations;
using SocialMediaAppBackend.DTOs.User;

public class CommentResponseDto
{
    public int Id { get; set; }
    [Required]
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int PostId { get; set; }
    [Required]
    public required UserResponseDto Author { get; set; }
}