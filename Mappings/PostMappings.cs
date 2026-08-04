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
            Author = UserMappings.ToResponseDto(post.Author),
            CreatedAt = post.CreatedAt,
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count
        };
    }
    
    public static Post ToPost(PostRequestDto postRequestDto, int authorId)
    {
        return new Post
        {
            Content = postRequestDto.Content,
            AuthorId = authorId
        };
    }
}