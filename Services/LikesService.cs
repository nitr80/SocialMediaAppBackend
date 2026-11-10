using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
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

    public async Task<Result<bool>> GetLike(int postId, int userId, CancellationToken cancellationToken = default)
    {
        bool liked = await _appDbContext.Likes
            .AnyAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        return Result<bool>.Ok(liked);
    }

    public async Task<Result<List<int>>> GetAllLikedPostIdsByUserId(int userId, CancellationToken cancellationToken = default)
    {
        List<int> likedPostIdList = await _appDbContext.Likes.Where(l => l.UserId == userId)
            .Select(l => l.PostId)
            .ToListAsync(cancellationToken);

        return Result<List<int>>.Ok(likedPostIdList);
    }

    public async Task<Result<bool>> LikePost(int postId, int userId, CancellationToken cancellationToken = default)
    {
        bool alreadyLiked = await _appDbContext.Likes
            .AnyAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (alreadyLiked)
        {
            return Result<bool>.Fail("Post already liked by the user", ErrorKind.Conflict);
        }

        Like like = new Like
        {
            UserId = userId,
            PostId = postId,
        };

        await _appDbContext.Likes.AddAsync(like, cancellationToken);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Fail("Post already liked by the user", ErrorKind.Conflict);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UnlikePost(int postId, int userId, CancellationToken cancellationToken = default)
    {
        Like? like = await _appDbContext.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (like == null)
        {
            return Result<bool>.Ok(true);
        }

        _appDbContext.Likes.Remove(like);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}