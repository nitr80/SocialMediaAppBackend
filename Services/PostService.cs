using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using SQLitePCL;

namespace SocialMediaAppBackend.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _appDbContext;

    public PostService(AppDbContext appDbContext)
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

    public async Task<Result<PostResponseDto>> DeletePostById(int id, int userId)
    {
        Post post = await _appDbContext.Posts.FirstAsync(p => p.Id == id);
        _appDbContext.Posts.Remove(post);
        await _appDbContext.SaveChangesAsync();

        if (post.AuthorId != userId)
        {
            return Result<PostResponseDto>.Fail("User not authorized!");
        }

        return Result<PostResponseDto>.Ok(PostMappings.ToResponseDto(post));
    }

    public async Task<Result<List<PostResponseDto>>> GetAllPosts()
    {
        List<Post> postList = await _appDbContext.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();
        // List<Post> postList = await _appDbContext.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();
        List<PostResponseDto> postResponseDtoList = new List<PostResponseDto>();
        
        foreach (Post post in postList)
        {
            postResponseDtoList.Add(PostMappings.ToResponseDto(post));
        }

        return Result<List<PostResponseDto>>.Ok(postResponseDtoList);
    }

    public async Task<Result<PostResponseDto>> GetPostById(int id)
    {
        Post post = await _appDbContext.Posts.FirstAsync(p => p.Id == id);

        if (post == null)
        {
            return Result<PostResponseDto>.Fail("User Null");
        }

        return Result<PostResponseDto>.Ok(PostMappings.ToResponseDto(post));
    }
    
}