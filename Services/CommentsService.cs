using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class CommentsService : ICommentsService
{
    private AppDbContext _appDbContext;

    public CommentsService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<bool>> CreateComment(Comment comment)
    {
        await _appDbContext.Comments.AddAsync(comment);
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<Comment>>> GetCommentsByPostId(int postId)
    {
        List<Comment> commentList = await _appDbContext.Comments
            .Where(c => c.ParentPostId == postId)
            .Include(c => c.Author)
            .Include(c => c.CommentLikes)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
            
        return Result<List<Comment>>.Ok(commentList);
    }
}