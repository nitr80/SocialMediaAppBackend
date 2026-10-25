using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IUsersService
{
    public Task<Result<User>> GetUserById(int id);
    public Task<Result<bool>> AddOrUpdateBio(string bio, int userId);
    public Task<Result<string>> AddOrUpdateProfilePicture(IFormFile image, int userId);
    public Task<Result<List<UserResponseDto>>> SearchUsername(string? search, int currentUserId);
}
