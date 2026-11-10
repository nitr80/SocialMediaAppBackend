using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface ILikesService
{
    public Task<Result<bool>> GetLike(int postId, int userId, CancellationToken cancellationToken = default);
    public Task<Result<List<int>>> GetAllLikedPostIdsByUserId(int userId, CancellationToken cancellationToken = default);
    public Task<Result<bool>> LikePost(int postId, int userId, CancellationToken cancellationToken = default);
    public Task<Result<bool>> UnlikePost(int postId, int userId, CancellationToken cancellationToken = default);
}
