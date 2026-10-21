using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Comment;

public class CommentRequestDto
{
    [Required]
    [MaxLength(1000)]
    public required string Content { get; set; }
}
