using System.Security.Claims;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostsService _postsService;

    public PostsController(IPostsService postsService)
    {
        _postsService = postsService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<PostResponseDto>>> GetAll()
    {
        Result<List<PostResponseDto>> result = await _postsService.GetAllPosts();

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(result.Data!);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetById(int id)
    {
        Result<Post> result = await _postsService.GetPostById(id);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(PostMappings.ToResponseDto(result.Data!));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostResponseDto>> Create(PostRequestDto postRequestDto)
    {
        Post post = PostMappings.ToPost(postRequestDto, User.GetUserId());

        Result<Post> result = await _postsService.CreatePost(post);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        PostResponseDto postResponseDto = PostMappings.ToResponseDto(result.Data!);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, postResponseDto);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        Result<bool> result = await _postsService.DeletePostById(id, User.GetUserId());

        return result.ToActionResult(this);
    }
}