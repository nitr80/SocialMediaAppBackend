using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class CommentLikesService : ICommentLikesService
{
    private readonly AppDbContext _appDbContext;
    public CommentLikesService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<List<int>>> GetAllLikedCommentIdsByUserIdAndPostId(int userId, int postId)
    {
        List<int> likedCommentIds = await _appDbContext.CommentLikes
            .Where(cl => cl.UserId == userId && cl.Comment.ParentPostId == postId)
            .Select(cl => cl.CommentId)
            .ToListAsync();

        return Result<List<int>>.Ok(likedCommentIds);
    }

    public async Task<Result<bool>> GetLike(int commentId, int userId)
    {
        bool isLiked = await _appDbContext.CommentLikes.AnyAsync(l => l.CommentId == commentId && l.UserId == userId);

        return Result<bool>.Ok(isLiked);
    }

    public async Task<Result<bool>> LikeComment(int commentId, int userId)
    {
        bool alreadyLiked = await _appDbContext.CommentLikes
            .AnyAsync(l => l.CommentId == commentId && l.UserId == userId);

        if (alreadyLiked)
        {
            return Result<bool>.Fail("Comment already liked by the user");
        }

        CommentLike commentLike = new CommentLike
        {
            UserId = userId,
            CommentId = commentId
        };

        await _appDbContext.CommentLikes.AddAsync(commentLike);
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UnlikeComment(int commentId, int userId)
    {
        CommentLike? commentLike = await _appDbContext.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

        if (commentLike == null)
        {
            return Result<bool>.Fail("Comment already unliked by the user");
        }

        _appDbContext.CommentLikes.Remove(commentLike);
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }
}