using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Data;
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
            return Result<User>.Fail("User not found", ErrorKind.NotFound);
        }

        return Result<User>.Ok(user);
    }

    public async Task<Result<bool>> AddOrUpdateBio(string bio, int userId)
    {
        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<bool>.Fail("User not found", ErrorKind.NotFound);
        }

        user.Bio = bio;
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<Result<bool>> AddOrUpdateProfilePicture(IFormFile image, int userId)
    {
        if (image == null || image.Length == 0)
        {
            return Result<bool>.Fail("Profile picture is required", ErrorKind.Validation);
        }

        if (image.Length > MaxImageSizeBytes)
        {
            return Result<bool>.Fail("Profile picture must be 5 MB or smaller", ErrorKind.Validation);
        }

        string extension = Path.GetExtension(image.FileName).ToLowerInvariant();

        if (!AllowedImageExtensions.Contains(extension))
        {
            return Result<bool>.Fail("Only jpg, jpeg, png and webp images are allowed", ErrorKind.Validation);
        }

        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<bool>.Fail("User not found", ErrorKind.NotFound);
        }

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

        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl) &&
            user.ProfileImageUrl.StartsWith("/images/profiles/"))
        {
            string oldFileName = Path.GetFileName(user.ProfileImageUrl);
            string oldFilePath = Path.Combine(folder, oldFileName);

            try
            {
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }
            catch
            {
                // Deletion failures should not block the profile picture update
            }
        }

        user.ProfileImageUrl = $"/images/profiles/{fileName}";
        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }
}