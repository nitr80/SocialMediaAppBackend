using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IUsersService
{
    public Task<Result<User>> GetUserById(int id, CancellationToken cancellationToken = default);
    public Task<Result<bool>> AddOrUpdateBio(string bio, int userId, CancellationToken cancellationToken = default);
    public Task<Result<string>> AddOrUpdateProfilePicture(IFormFile image, int userId, CancellationToken cancellationToken = default);
    public Task<Result<List<UserPublicDto>>> SearchUsername(string? search, int currentUserId, CancellationToken cancellationToken = default);
}
