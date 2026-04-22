using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface ILikesService
{
    public Task<Result<bool>> LikePost(int postId, int userId);
    public Task<Result<bool>> UnlikePost(int postId, int userId);
}