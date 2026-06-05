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
        User user = await _appDbContext.Users.FirstAsync(u => u.Id == id);

        return Result<User>.Ok(user);
    }
}