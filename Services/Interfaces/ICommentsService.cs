using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface ICommentsService
{
    public Task<Result<Comment>> CreateComment(Comment comment, CancellationToken cancellationToken = default);
    public Task<Result<List<CommentResponseDto>>> GetCommentsByPostId(int postId, CancellationToken cancellationToken = default);
}
