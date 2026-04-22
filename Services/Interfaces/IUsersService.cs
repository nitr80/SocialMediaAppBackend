using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IUsersService
{
    public Task<Result<UserResponseDto>> GetUserById(int id);
}