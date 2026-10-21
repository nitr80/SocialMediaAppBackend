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
        if (!await _appDbContext.Posts.AnyAsync(p => p.Id == comment.ParentPostId))
        {
            return Result<Comment>.Fail("Post doesn't exist", ErrorKind.NotFound);
        }

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
        if (!await _appDbContext.Posts.AnyAsync(p => p.Id == postId))
        {
            return Result<List<CommentResponseDto>>.Fail("Post doesn't exist", ErrorKind.NotFound);
        }

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