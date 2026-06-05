using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface ICommentsService
{
    public Task<Result<List<Comment>>> GetCommentsByPostId(int postId);
    public Task<Result<bool>> CreateComment(Comment comment);
}