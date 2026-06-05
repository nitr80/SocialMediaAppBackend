using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using SQLitePCL;

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

        return Result<Post>.Ok(post);
    }

    public async Task<Result<bool>> DeletePostById(int postId, int userId)
    {
        int affected = await _appDbContext.Posts
            .Where(p => p.Id == postId && p.AuthorId == userId)
            .ExecuteDeleteAsync();

        if (affected == 0)
        {
            return Result<bool>.Fail("Not found or not authorized");
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<Post>>> GetAllPosts()
    {
        List<Post> postList = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p =>  p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        return Result<List<Post>>.Ok(postList);
    }

    public async Task<Result<Post>> GetPostById(int postId)
    {
        Post post = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p =>  p.Likes)
            .FirstAsync(p => p.Id == postId);

        return Result<Post>.Ok(post);
    }
    
}