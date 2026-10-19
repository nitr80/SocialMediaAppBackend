using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Post;

public class PostRequestDto
{
    [Required]
    [MaxLength(5000)]
    public required string Content { get; set; }
}