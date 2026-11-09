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

    private static async Task<bool> HasValidImageSignature(IFormFile image, string extension)
    {
        try
        {
            byte[] header = new byte[12];
            await using Stream stream = image.OpenReadStream();
            int bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));

            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                     header[0] == 0xFF &&
                                     header[1] == 0xD8 &&
                                     header[2] == 0xFF,
                ".png" => bytesRead >= 8 &&
                          header[0] == 0x89 &&
                          header[1] == 0x50 &&
                          header[2] == 0x4E &&
                          header[3] == 0x47 &&
                          header[4] == 0x0D &&
                          header[5] == 0x0A &&
                          header[6] == 0x1A &&
                          header[7] == 0x0A,
                ".webp" => bytesRead >= 12 &&
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<string>> AddOrUpdateProfilePicture(IFormFile image, int userId)
    {
        if (image == null || image.Length == 0)
        {
            return Result<string>.Fail("Profile picture is required", ErrorKind.Validation);
        }

        if (image.Length > MaxImageSizeBytes)
        {
            return Result<string>.Fail("Profile picture must be 5 MB or smaller", ErrorKind.Validation);
        }

        string extension = Path.GetExtension(image.FileName).ToLowerInvariant();

        if (!AllowedImageExtensions.Contains(extension))
        {
            return Result<string>.Fail("Only jpg, jpeg, png and webp images are allowed", ErrorKind.Validation);
        }

        if (!await HasValidImageSignature(image, extension))
        {
            return Result<string>.Fail("The file content does not match a valid image format", ErrorKind.Validation);
        }

        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<string>.Fail("User not found", ErrorKind.NotFound);
        }

        string fileName = $"{Guid.NewGuid()}{extension}";

        string folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "images",
            "profiles");

        Directory.CreateDirectory(folder);

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

        return Result<string>.Ok(user.ProfileImageUrl);
    }

    public async Task<Result<List<UserPublicDto>>> SearchUsername(string? search, int currentUserId)
    {
        IQueryable<User> query = _appDbContext.Users;

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (_appDbContext.Database.IsNpgsql())
            {
                query = query.Where(u => EF.Functions.ILike(u.Username, $"%{search}%") && u.Id != currentUserId);
            }
            else
            {
                query = query.Where(u => u.Username.ToLower().Contains(search.ToLower()) && u.Id != currentUserId);
            }

            query = query.OrderBy(u => u.Username).Take(10);
        }
        else
        {
            // TODO: Randomize
            query = query.Where(u => u.Id != currentUserId).Take(10);
        }

        List<UserPublicDto> users = await query
            .Select(u => UserMappings.ToPublicDto(u))
            .ToListAsync();

        return Result<List<UserPublicDto>>.Ok(users);
    }
}