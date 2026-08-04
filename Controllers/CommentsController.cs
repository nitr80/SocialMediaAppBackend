namespace SocialMediaAppBackend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.Comment;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using SocialMediaAppBackend.Services.Interfaces;

[ApiController]
[Route("[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _commentsService;

    public CommentsController(ICommentsService commentsService)
    {
        _commentsService = commentsService;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Create(CommentRequestDto commentRequestDto)
    {
        Comment comment = CommentMappings.ToComment(commentRequestDto, User.GetUserId());
        Result<bool> result = await _commentsService.CreateComment(comment);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok();
    }

    [Authorize]
    [HttpGet("{postId}")]
    public async Task<ActionResult<List<CommentResponseDto>>> GetAllByPostId(int postId)
    {
        Result<List<Comment>> result = await _commentsService.GetCommentsByPostId(postId);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        List<CommentResponseDto> commentResponseDtoList = new List<CommentResponseDto>();
        
        foreach (Comment comment in result.Data!)         // Cannot be null because of success
        {
            commentResponseDtoList.Add(CommentMappings.ToResponseDto(comment));
        }

        return Ok(commentResponseDtoList);
    }
}