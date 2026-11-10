using Microsoft.AspNetCore.Authorization;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("Comments/{commentId}/Likes")]
[Authorize]
public class CommentLikesController : ControllerBase
{
    private readonly ICommentLikesService _commentLikesService;

    public CommentLikesController(ICommentLikesService commentLikesService)
    {
        _commentLikesService = commentLikesService;
    }

    [HttpGet]
    public async Task<ActionResult<bool>> Get(int commentId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _commentLikesService.GetLike(commentId, User.GetUserId(), cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("/Comments/me/{postId}/Likes")]
    public async Task<ActionResult<List<int>>> GetAllByPostId(int postId, CancellationToken cancellationToken = default)
    {
        Result<List<int>> result = await _commentLikesService.GetAllLikedCommentIdsByUserIdAndPostId(User.GetUserId(), postId, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<ActionResult> LikeResource(int commentId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _commentLikesService.LikeComment(commentId, User.GetUserId(), cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete]
    public async Task<ActionResult> UnlikeResource(int commentId, CancellationToken cancellationToken = default)
    {
        Result<bool> result = await _commentLikesService.UnlikeComment(commentId, User.GetUserId(), cancellationToken);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }
}