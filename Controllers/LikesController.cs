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

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("liked")]
    public async Task<ActionResult<List<int>>> GetAll()
    {
        Result<List<int>> result = await _likesService.GetAllLikedPostIdsByUserId(User.GetUserId());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("like/{postId}")]
    public async Task<ActionResult> LikePost(int postId)
    {
        Result<bool> result = await _likesService.LikePost(postId, User.GetUserId());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("unlike/{postId}")]
    public async Task<ActionResult> UnlikePost(int postId)
    {
        Result<bool> result = await _likesService.UnlikePost(postId, User.GetUserId());

        return result.ToActionResult(this);
    }
}