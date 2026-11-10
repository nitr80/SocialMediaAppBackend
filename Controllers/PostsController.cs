using System.Security.Claims;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using SocialMediaAppBackend.DTOs;

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
    public async Task<ActionResult<PagedList<PostResponseDto>>> GetAll([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken = default)
    {
        Result<PagedList<PostResponseDto>> result = await _postsService.GetAllPosts(User.GetUserId(), limit, cursor, cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(result.Data!);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        Result<Post> result = await _postsService.GetPostById(id, User.GetUserId(), cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(PostMappings.ToResponseDto(result.Data!, User.GetUserId()));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostResponseDto>> Create(PostRequestDto postRequestDto, CancellationToken cancellationToken = default)
    {
        Post post = PostMappings.ToPost(postRequestDto, User.GetUserId());

        Result<Post> result = await _postsService.CreatePost(post, cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        PostResponseDto postResponseDto = PostMappings.ToResponseDto(result.Data!, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, postResponseDto);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _postsService.DeletePostById(id, User.GetUserId(), cancellationToken);

        return result.ToActionResult(this);
    }
}