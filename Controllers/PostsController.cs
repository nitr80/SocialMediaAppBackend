using System.Security.Claims;
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
        Result<List<Post>> result = await _postsService.GetAllPosts();

        if (!result.Success)
        {
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        List<PostResponseDto> postResponseDtoList = new List<PostResponseDto>();
        
        foreach (Post post in result.Data!)         // Cannot be null because of success
        {
            postResponseDtoList.Add(PostMappings.ToResponseDto(post));
        }

        return Ok(postResponseDtoList);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetById(int id)
    {
        Result<Post> result = await _postsService.GetPostById(id);

        if (!result.Success)
        {
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        return Ok(PostMappings.ToResponseDto(result.Data!));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Create(PostRequestDto postRequestDto)
    {
        Post post = PostMappings.ToPost(postRequestDto, User.GetUserId());

        Result<Post> result = await _postsService.CreatePost(post);

        if (!result.Success)
        {
            // result.Success is always true since middleware will handle db errors
            return BadRequest(result.Error);
        }

        return Ok();
    } 

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        Result<bool> result = await _postsService.DeletePostById(id, User.GetUserId());

        if (!result.Success)
        {
            return Unauthorized(result.Error);
        }

        return Ok();
    }
}