using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class FollowsService : IFollowsService
{
    private readonly AppDbContext _appDbContext;

    public FollowsService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<bool>> FollowUser(int followedUserId, int followerId)
    {
        var userExists = await _appDbContext.Users.AnyAsync(u => u.Id == followedUserId);
        if (!userExists)
        {
            return Result<bool>.Fail("User doesn't exist", ErrorKind.NotFound);
        }

        var alreadyFollowing = await _appDbContext.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        if (alreadyFollowing)
        {
            return Result<bool>.Fail("Already following this user", ErrorKind.Conflict);
        }

        var follow = new Follow
        {
            FollowerId = followerId,
            FollowedUserId = followedUserId
        };

        await _appDbContext.Follows.AddAsync(follow);
        await _appDbContext.SaveChangesAsync();
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> GetFollowingStatus(int followedUserId, int followerId)
    {
        var userExists = await _appDbContext.Users.AnyAsync(u => u.Id == followedUserId);
        if (!userExists)
        {
            return Result<bool>.Fail("User doesn't exist", ErrorKind.NotFound);
        }

        var isFollowing = await _appDbContext.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        return Result<bool>.Ok(isFollowing);
    }

    public async Task<Result<bool>> UnfollowUser(int followedUserId, int followerId)
    {
        var userExists = await _appDbContext.Users.AnyAsync(u => u.Id == followedUserId);
        if (!userExists)
        {
            return Result<bool>.Fail("User doesn't exist", ErrorKind.NotFound);
        }

        var follow = await _appDbContext.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        if (follow == null)
        {
            return Result<bool>.Ok(true);
        }

        _appDbContext.Follows.Remove(follow);
        await _appDbContext.SaveChangesAsync();
        return Result<bool>.Ok(true);
    }
}