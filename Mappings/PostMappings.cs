using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Models;

namespace SocialMediaAppBackend.Mappings;

public static class PostMappings
{
    public static PostResponseDto ToResponseDto(Post post)
    {
        return new PostResponseDto
        {
            Id = post.Id,
            Content = post.Content,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            LikeCount = post.LikeCount
        };
    }
}