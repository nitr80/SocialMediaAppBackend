using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
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

    public async Task<Result<Comment>> CreateComment(Comment comment)
    {
        await _appDbContext.Comments.AddAsync(comment);
        await _appDbContext.SaveChangesAsync();

        comment = await _appDbContext.Comments
            .Include(c => c.Author)
            .Include(c => c.CommentLikes)
            .FirstAsync(c => c.Id == comment.Id);

        return Result<Comment>.Ok(comment);
    }

    public async Task<Result<List<CommentResponseDto>>> GetCommentsByPostId(int postId)
    {
        List<CommentResponseDto> commentList = await _appDbContext.Comments
            .Where(c => c.ParentPostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                PostId = c.ParentPostId,
                LikeCount = c.CommentLikes.Count(),
                Author = new DTOs.User.UserResponseDto
                {
                    Id = c.Author.Id,
                    Username = c.Author.Username,
                    Email = c.Author.Email,
                    Bio = c.Author.Bio,
                    ProfileImageUrl = c.Author.ProfileImageUrl
                }
            })
            .ToListAsync();

        return Result<List<CommentResponseDto>>.Ok(commentList);
    }
}