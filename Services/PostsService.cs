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

    public async Task<Result<PostResponseDto>> CreatePost(PostRequestDto postRequestDto, int userId)
    {
        User user = await _appDbContext.Users.FirstAsync(u => userId == u.Id);

        Post post = new Post
        {
            Content = postRequestDto.Content,
            Author = user
        };

        await _appDbContext.Posts.AddAsync(post);
        await _appDbContext.SaveChangesAsync();

        return Result<PostResponseDto>.Ok(PostMappings.ToResponseDto(post));
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

    public async Task<Result<List<PostResponseDto>>> GetAllPosts()
    {
        List<Post> postList = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p =>  p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        List<PostResponseDto> postResponseDtoList = new List<PostResponseDto>();
        
        foreach (Post post in postList)
        {
            postResponseDtoList.Add(PostMappings.ToResponseDto(post));
        }

        return Result<List<PostResponseDto>>.Ok(postResponseDtoList);
    }

    public async Task<Result<PostResponseDto>> GetPostById(int postId)
    {
        Post post = await _appDbContext.Posts
            .Include(p => p.Author)
            .Include(p =>  p.Likes)
            .FirstAsync(p => p.Id == postId);

        return Result<PostResponseDto>.Ok(PostMappings.ToResponseDto(post));
    }
    
}