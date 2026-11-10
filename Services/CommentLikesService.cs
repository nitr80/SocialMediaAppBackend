using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
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

    public async Task<Result<List<int>>> GetAllLikedCommentIdsByUserIdAndPostId(int userId, int postId, CancellationToken cancellationToken = default)
    {
        List<int> likedCommentIds = await _appDbContext.CommentLikes
            .Where(cl => cl.UserId == userId && cl.Comment.ParentPostId == postId)
            .Select(cl => cl.CommentId)
            .ToListAsync(cancellationToken);

        return Result<List<int>>.Ok(likedCommentIds);
    }

    public async Task<Result<bool>> GetLike(int commentId, int userId, CancellationToken cancellationToken = default)
    {
        bool isLiked = await _appDbContext.CommentLikes.AnyAsync(l => l.CommentId == commentId && l.UserId == userId, cancellationToken);

        return Result<bool>.Ok(isLiked);
    }

    public async Task<Result<bool>> LikeComment(int commentId, int userId, CancellationToken cancellationToken = default)
    {
        bool alreadyLiked = await _appDbContext.CommentLikes
            .AnyAsync(l => l.CommentId == commentId && l.UserId == userId, cancellationToken);

        if (alreadyLiked)
        {
            return Result<bool>.Fail("Comment already liked by the user", ErrorKind.Conflict);
        }

        CommentLike commentLike = new CommentLike
        {
            UserId = userId,
            CommentId = commentId
        };

        await _appDbContext.CommentLikes.AddAsync(commentLike, cancellationToken);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Fail("Comment already liked by the user", ErrorKind.Conflict);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UnlikeComment(int commentId, int userId, CancellationToken cancellationToken = default)
    {
        CommentLike? commentLike = await _appDbContext.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId, cancellationToken);

        if (commentLike == null)
        {
            return Result<bool>.Ok(true);
        }

        _appDbContext.CommentLikes.Remove(commentLike);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}