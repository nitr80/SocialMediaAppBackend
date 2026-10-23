using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;


public interface IFollowsService
{
    Task<Result<bool>> FollowUser(int followedUserId, int followerId);
    Task<Result<bool>> UnfollowUser(int followedUserId, int followerId);
    Task<Result<bool>> GetFollowingStatus(int followedUserId, int followerId);
}