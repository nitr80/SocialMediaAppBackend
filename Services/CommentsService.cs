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

    public async Task<Result<Comment>> CreateComment(Comment comment, CancellationToken cancellationToken = default)
    {
        if (!await _appDbContext.Posts.AnyAsync(p => p.Id == comment.ParentPostId, cancellationToken))
        {
            return Result<Comment>.Fail("Post doesn't exist", ErrorKind.NotFound);
        }

        await _appDbContext.Comments.AddAsync(comment, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        comment = await _appDbContext.Comments
            .Include(c => c.Author)
            .Include(c => c.CommentLikes)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);

        return Result<Comment>.Ok(comment);
    }

    public async Task<Result<List<CommentResponseDto>>> GetCommentsByPostId(int postId, CancellationToken cancellationToken = default)
    {
        if (!await _appDbContext.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
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
                Author = new DTOs.User.UserPublicDto
                {
                    Id = c.Author.Id,
                    Username = c.Author.Username,
                    Bio = c.Author.Bio,
                    ProfileImageUrl = c.Author.ProfileImageUrl
                }
            })
            .ToListAsync(cancellationToken);

        return Result<List<CommentResponseDto>>.Ok(commentList);
    }
}