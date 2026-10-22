using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

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

    public async Task<Result<List<PostResponseDto>>> GetAllPosts(int currentUserId)
    {
        List<PostResponseDto> postList = await _appDbContext.Posts
            .Where(p => p.AuthorId == currentUserId || 
                _appDbContext.Follows.Any(
                    f => f.UserId == currentUserId && 
                    f.FollowedUserId == p.AuthorId))
            .OrderByDescending(p => p.CreatedAt)
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
            .ToListAsync();

        return Result<List<PostResponseDto>>.Ok(postList);
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
            .AnyAsync(f => f.UserId == currentUserId && f.FollowedUserId == post.AuthorId);
        
        if (!isFollowing && post.AuthorId != currentUserId)
        {
            return Result<Post>.Fail("Not authorized to view this post", ErrorKind.Forbidden);
        }

        return Result<Post>.Ok(post);
    }

}