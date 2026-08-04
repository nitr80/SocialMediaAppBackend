using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("[controller]")]
public class CommentLikesController : ControllerBase
{
    private readonly ICommentLikesService _commentLikesService;

    public CommentLikesController(ICommentLikesService commentLikesService)
    {
        _commentLikesService = commentLikesService;
    }

    [Authorize]
    [HttpGet("liked/{commentId}")]
    public async Task<ActionResult<bool>> Get(int commentId)
    {
        Result<bool> result = await _commentLikesService.GetLike(commentId, User.GetUserId());

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("{postId}/liked")]
    public async Task<ActionResult<List<int>>> GetAllByPostId(int postId)
    {
        Result<List<int>> result = await _commentLikesService.GetAllLikedCommentIdsByUserIdAndPostId(User.GetUserId(), postId);

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("like/{commentId}")]
    public async Task<ActionResult<bool>> Like(int commentId)
    {
        await _commentLikesService.LikeComment(commentId, User.GetUserId());

        return Ok();
    }

    [Authorize]
    [HttpPost("unlike/{commentId}")]
    public async Task<ActionResult<bool>> Unlike(int commentId)
    {
        await _commentLikesService.UnlikeComment(commentId, User.GetUserId());

        return Ok();
    }
}