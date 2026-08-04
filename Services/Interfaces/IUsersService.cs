using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IUsersService
{
    public Task<Result<User>> GetUserById(int id);
    public Task<Result<bool>> AddOrUpdateBio(string bio, int userId);
    public  Task<Result<bool>> AddOrUpdateProfilePicture(IFormFile image, int userId);
}