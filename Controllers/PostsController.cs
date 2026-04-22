using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.Post;
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
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetById(int id)
    {
        Result<PostResponseDto> result = await _postsService.GetPostById(id);

        if (!result.Success)
        {
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostResponseDto>> Create(PostRequestDto postRequestDto)
    {
        Result<PostResponseDto> result = await _postsService.CreatePost(postRequestDto, GetUserId());

        if (!result.Success)
        {
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    } 

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        Result<PostResponseDto> result = await _postsService.DeletePostById(id, GetUserId());

        if (!result.Success)
        {
            return Unauthorized(result.Error);
        }

        return Ok();
    }

    private int GetUserId()
    {
        string? value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(value, out int userId))
        {
            throw new Exception("Invalid token user id");
        }

        return userId;
    }
}