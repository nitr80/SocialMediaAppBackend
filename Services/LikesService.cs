using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class LikesService : ILikesService
{
    private readonly AppDbContext _appDbContext;

    public LikesService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<bool>> LikePost(int postId, int userId)
    {
        bool alreadyLiked = await _appDbContext.Likes
            .AnyAsync(l => l.PostId == postId && l.UserId == userId);

        if (alreadyLiked)
        {
            return Result<bool>.Fail("Post already liked by the user");
        }

        Like like = new Like
        {
            UserId = userId,
            PostId = postId,
        };

        await _appDbContext.Likes.AddAsync(like);
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UnlikePost(int postId, int userId)
    {
        Like? like = await _appDbContext.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (like == null)
        {
            return Result<bool>.Fail("Post is already not liked by the user");
        }

        _appDbContext.Likes.Remove(like);
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }
}