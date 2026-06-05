using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("[controller]")]
public class LikesController : ControllerBase
{
    private readonly ILikesService _likesService;

    public LikesController(ILikesService likesService)
    {
        _likesService = likesService;
    }

    [Authorize]
    [HttpGet("liked/{postId}")]
    public async Task<ActionResult<bool>> Get(int postId)
    {
        Result<bool> result = await _likesService.GetLike(postId, User.GetUserId());

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("liked")]
    public async Task<ActionResult<List<int>>> GetAll()
    {
        Result<List<int>> result = await _likesService.GetAllLikedPostIdsByUserId(User.GetUserId());

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("like/{postId}")]
    public async void LikePost(int postId)
    {
        await _likesService.LikePost(postId, User.GetUserId());
    }

    [Authorize]
    [HttpPost("unlike/{postId}")]
    public async void UnlikePost(int postId)
    {
        await _likesService.UnlikePost(postId, User.GetUserId());
    }
}