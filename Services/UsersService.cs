using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Mappings;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class UsersService : IUsersService
{
    private readonly AppDbContext _appDbContext;

    public UsersService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<User>> GetUserById(int id)
    {
        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return Result<User>.Fail("User not found");
        }

        return Result<User>.Ok(user);
    }

    public async Task<Result<bool>> AddOrUpdateBio(string bio, int userId)
    {
        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<bool>.Fail("User not found");
        }

        user.Bio = bio;
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> AddOrUpdateProfilePicture(IFormFile image, int userId)
    {
        if (image == null)
        {
            return Result<bool>.Fail("Profile picture is null");
        }

        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<bool>.Fail("User not found");
        }

        string extension = Path.GetExtension(image.FileName);
        string fileName = $"{Guid.NewGuid()}{extension}";

        string folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "images",
            "profiles");

        string filePath = Path.Combine(folder, fileName);

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        user.ProfileImageUrl = $"/images/profiles/{fileName}";
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }
}