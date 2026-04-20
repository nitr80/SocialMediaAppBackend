using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.Post;

public class PostRequestDto
{
    [Required]
    public required string Content { get; set; }
}