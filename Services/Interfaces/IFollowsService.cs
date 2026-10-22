using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;


public interface IFollowsService
{
    Task<Result<bool>> FollowUser(int userId, int followerId);
    Task<Result<bool>> UnfollowUser(int userId, int followerId);
    Task<Result<bool>> GetFollowingStatus(int userId, int followerId);
}