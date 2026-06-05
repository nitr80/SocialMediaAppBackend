using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IUsersService
{
    public Task<Result<User>> GetUserById(int id);
}