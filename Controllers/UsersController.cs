using Microsoft.AspNetCore.Authorization;
using SocialMediaAppBackend.Extensions;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        Result<User> result = await _usersService.GetUserById(id);

        if (!result.Success)
        {
            return result.ToActionResult(this);
        }

        return Ok(UserMappings.ToResponseDto(result.Data!));
    }

    [Authorize]
    [HttpPatch("me/Bio")]
    public async Task<ActionResult> AddOrUpdateBio(UserBioRequestDto userBioRequestDto)
    {
        Result<bool> result = await _usersService.AddOrUpdateBio(UserMappings.ToBioString(userBioRequestDto), User.GetUserId());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPatch("me/ProfilePicture")]
    public async Task<ActionResult> AddOrUpdateProfilePicture(IFormFile image)
    {
        Result<bool> result = await _usersService.AddOrUpdateProfilePicture(image, User.GetUserId());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<UserResponseDto>>> SearchUsername([FromQuery] string? search)
    {
        Result<List<UserResponseDto>> result = await _usersService.SearchUsername(search, User.GetUserId());

        return result.ToActionResult(this);
    }
}