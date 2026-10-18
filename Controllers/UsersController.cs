using Microsoft.AspNetCore.Authorization;
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
    [HttpPatch("update-bio")]
    public async Task<ActionResult> AddOrUpdateBio(UserBioRequestDto userBioRequestDto)
    {
        Result<bool> result = await _usersService.AddOrUpdateBio(UserMappings.ToBioString(userBioRequestDto), User.GetUserId());

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPatch("update-profile-picture")]
    public async Task<ActionResult> AddOrUpdateProfilePicture(IFormFile image)
    {
        Result<bool> result = await _usersService.AddOrUpdateProfilePicture(image, User.GetUserId());

        return result.ToActionResult(this);
    }
}