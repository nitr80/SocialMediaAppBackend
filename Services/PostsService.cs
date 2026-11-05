using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using System.Text.Json;
using System.Text;
using SocialMediaAppBackend.DTOs;

namespace SocialMediaAppBackend.Services;

public class PostsService : IPostsService
{
    private readonly AppDbContext _appDbContext;

    public PostsService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<Post>> CreatePost(Post post)
    {
        await _appDbContext.Posts.AddAsync(post);
        await _appDbContext.SaveChangesAsync();

        Post createdPost = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstAsync(p => p.Id == post.Id);

        return Result<Post>.Ok(createdPost);
    }

    public async Task<Result<bool>> DeletePostById(int postId, int userId)
    {
        bool postExists = await _appDbContext.Posts.AnyAsync(p => p.Id == postId);

        if (!postExists)
        {
            return Result<bool>.Fail("Post not found", ErrorKind.NotFound);
        }

        int affected = await _appDbContext.Posts
            .Where(p => p.Id == postId && p.AuthorId == userId)
            .ExecuteDeleteAsync();

        if (affected == 0)
        {
            return Result<bool>.Fail("Not authorized", ErrorKind.Forbidden);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedList<PostResponseDto>>> GetAllPosts(
        int currentUserId,
        int? limit,
        string? cursor)
    {
        limit ??= 15;

        if (limit <= 0 || limit > 100)
        {
            return Result<PagedList<PostResponseDto>>.Fail("Limit must be between 1 and 100");
        }

        PostCursor? postCursor = null;

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            try
            {
                postCursor = PostCursor.DecodePostCursor(cursor);
            }
            catch
            {
                return Result<PagedList<PostResponseDto>>.Fail("Invalid cursor");
            }
        }

        var query = _appDbContext.Posts
            .Where(p => p.AuthorId == currentUserId ||
                _appDbContext.Follows.Any(f =>
                    f.FollowerId == currentUserId &&
                    f.FollowedUserId == p.AuthorId));

        if (postCursor != null)
        {
            query = query.Where(p =>
                p.CreatedAt < postCursor.CreatedAt ||
                (p.CreatedAt == postCursor.CreatedAt &&
                p.Id < postCursor.Id));
        }

        var postList = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PostResponseDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(),
                Author = new DTOs.User.UserResponseDto
                {
                    Id = p.Author.Id,
                    Username = p.Author.Username,
                    Email = p.Author.Email,
                    Bio = p.Author.Bio,
                    ProfileImageUrl = p.Author.ProfileImageUrl
                }
            })
            .Take(limit.Value + 1)
            .ToListAsync();

        bool hasMore = postList.Count > limit.Value;

        if (hasMore)
        {
            postList.RemoveAt(postList.Count - 1);
        }

        var lastPost = postList.LastOrDefault();

        string? nextCursor = hasMore && lastPost != null
            ? PostCursor.EncodePostCursor(new PostCursor(lastPost.CreatedAt, lastPost.Id))
            : null;

        var pagedList = new PagedList<PostResponseDto>
        {
            Values = postList,
            Cursor = nextCursor
        };

        return Result<PagedList<PostResponseDto>>.Ok(pagedList);
    }

    public async Task<Result<Post>> GetPostById(int postId, int currentUserId)
    {
        Post? post = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return Result<Post>.Fail("Post not found", ErrorKind.NotFound);
        }

        bool isFollowing = await _appDbContext.Follows
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowedUserId == post.AuthorId);

        if (!isFollowing && post.AuthorId != currentUserId)
        {
            return Result<Post>.Fail("Not authorized to view this post", ErrorKind.Forbidden);
        }

        return Result<Post>.Ok(post);
    }

}