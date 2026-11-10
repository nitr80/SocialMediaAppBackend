using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface ICommentLikesService
{
    public Task<Result<bool>> GetLike(int commentId, int userId, CancellationToken cancellationToken = default);
    public Task<Result<List<int>>> GetAllLikedCommentIdsByUserIdAndPostId(int userId, int postId, CancellationToken cancellationToken = default);
    public Task<Result<bool>> LikeComment(int commentId, int userId, CancellationToken cancellationToken = default);
    public Task<Result<bool>> UnlikeComment(int commentId, int userId, CancellationToken cancellationToken = default);
}
