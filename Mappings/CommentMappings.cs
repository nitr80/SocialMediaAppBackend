using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.Models;

namespace SocialMediaAppBackend.Mappings;

public static class CommentMappings
{
    public static Comment ToComment(CommentRequestDto commentRequestDto, int authorId)
    {
        return new Comment
        {
            Content = commentRequestDto.Content,
            ParentPostId = commentRequestDto.PostId,
            AuthorId = authorId
        };
    }

    public static CommentResponseDto ToResponseDto(Comment comment)
    {
        return new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            LikeCount = comment.CommentLikes.Count(),
            PostId = comment.ParentPostId,
            Author = UserMappings.ToResponseDto(comment.Author)
        };
    }
}