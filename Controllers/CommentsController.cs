namespace SocialMediaAppBackend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.Extensions;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("Posts/{postId}/Comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _commentsService;

    public CommentsController(ICommentsService commentsService)
    {
        _commentsService = commentsService;
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponseDto>> CreateNested(int postId, CommentRequestDto commentRequestDto)
    {
        Comment comment = CommentMappings.ToComment(commentRequestDto, postId, User.GetUserId());
        Result<Comment> result = await _commentsService.CreateComment(comment);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        CommentResponseDto commentResponseDto = CommentMappings.ToResponseDto(result.Data!);
        return CreatedAtAction(
            nameof(GetAllByPostIdNested),
            new { postId = result.Data!.ParentPostId },
            commentResponseDto);
    }

    [HttpGet]
    public async Task<ActionResult<List<CommentResponseDto>>> GetAllByPostIdNested(int postId)
    {
        Result<List<CommentResponseDto>> result = await _commentsService.GetCommentsByPostId(postId);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(result.Data!);
    }
}