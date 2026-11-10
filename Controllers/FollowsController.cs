using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SocialMediaAppBackend.Services.Interfaces;
using SocialMediaAppBackend.Extensions;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Controllers;

[ApiController]
[Route("/Users/{userId}/[controller]")]
[Authorize]
public class FollowsController : ControllerBase
{
    private readonly IFollowsService _followsService;

    public FollowsController(IFollowsService followsService)
    {
        _followsService = followsService;
    }

    [HttpPost]
    public async Task<ActionResult> FollowUser(int userId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _followsService.FollowUser(userId, User.GetUserId(), cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete]
    public async Task<ActionResult> UnfollowUser(int userId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _followsService.UnfollowUser(userId, User.GetUserId(), cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<bool>> GetFollowingStatus(int userId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _followsService.GetFollowingStatus(userId, User.GetUserId(), cancellationToken);

        return result.ToActionResult(this);
    }
}
