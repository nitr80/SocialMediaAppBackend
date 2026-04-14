using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Options;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _appDbContext;
    private readonly JwtOptions _jwtOptions;

    private int tokenLifetimeInMinutes = 60;

    public AuthService(AppDbContext appDbContext, IOptions<JwtOptions> options)
    {
        _appDbContext = appDbContext;
        _jwtOptions = options.Value;
    }

    public async Task<AuthResult> LoginAsync(LoginDto loginDto)
    {
        User? user = await _appDbContext.Users.FirstOrDefaultAsync(user => user.Username == loginDto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = false,
                Error = "Invalid credentials"
            };
        }

        
        AuthResponseDto authResponseDto = new AuthResponseDto
        {
            Token = GenerateAuthJwt(user),
            User = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            }
        };

        return new AuthResult
        {
            Success = true,
            Data = authResponseDto
        };
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
    {
        if (await _appDbContext.Users.AnyAsync(user => user.Email == registerDto.Email || user.Username == registerDto.Username))
        {
            return new AuthResult
            {
                Success = false,
                Error = "User with this email or username already exists"
            };
        }

        User user = new User
        {
            Email = registerDto.Email,
            Username = registerDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        };

        await _appDbContext.Users.AddAsync(user);
        await _appDbContext.SaveChangesAsync();

        AuthResponseDto authResponseDto = new AuthResponseDto
        {
            Token = GenerateAuthJwt(user),
            User = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            }
        };

        Console.WriteLine(user.PasswordHash);

        return new AuthResult
        {
            Success = true,
            Data = authResponseDto
        };
    }

    private string GenerateAuthJwt(User user)
    {
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username.ToString()),
        };

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer, 
            audience: _jwtOptions.Audience, 
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenLifetimeInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}