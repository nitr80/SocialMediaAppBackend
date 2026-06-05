namespace SocialMediaAppBackend.DTOs.Comment;

using System.ComponentModel.DataAnnotations;

public class CommentRequestDto
{
    [Required]
    public required string Content { get; set; }
    public int PostId { get; set; }
}