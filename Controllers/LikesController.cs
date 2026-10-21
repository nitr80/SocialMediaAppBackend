using Microsoft.AspNetCore.Authorization;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("Posts/{postId}/Likes")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly ILikesService _likesService;

    public LikesController(ILikesService likesService)
    {
        _likesService = likesService;
    }

    [HttpGet]
    public async Task<ActionResult<bool>> Get(int postId)
    {
        Result<bool> result = await _likesService.GetLike(postId, User.GetUserId());

        return result.ToActionResult(this);
    }

    [HttpGet("/Posts/me/Likes")]
    public async Task<ActionResult<List<int>>> GetAll()
    {
        Result<List<int>> result = await _likesService.GetAllLikedPostIdsByUserId(User.GetUserId());

        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<ActionResult> LikePostResource(int postId)
    {
        Result<bool> result = await _likesService.LikePost(postId, User.GetUserId());

        return result.ToActionResult(this);
    }

    [HttpDelete]
    public async Task<ActionResult> UnlikePostResource(int postId)
    {
        Result<bool> result = await _likesService.UnlikePost(postId, User.GetUserId());

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }
}