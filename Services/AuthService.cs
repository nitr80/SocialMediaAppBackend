using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.DTOs.Auth;
using SocialMediaAppBackend.Mappings;
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

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        string username = loginDto.Username.ToLower();

        User? user = await _appDbContext.Users.FirstOrDefaultAsync(user => user.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Fail("Invalid credentials", ErrorKind.Unauthorized);
        }

        // Drop this user's expired tokens so the table does not grow forever
        await _appDbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.Expires < DateTime.UtcNow)
            .ExecuteDeleteAsync();

        string refreshToken = GenerateRefreshToken();

        RefreshToken refreshTokenEntity = new RefreshToken
        {
            Token = HashToken(refreshToken),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        await _appDbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _appDbContext.SaveChangesAsync();

        AuthResponseDto authResponseDto = new AuthResponseDto
        {
            AccessToken = GenerateAuthJwt(user),
            RefreshToken = refreshToken,
            User = UserMappings.ToResponseDto(user)
        };

        return Result<AuthResponseDto>.Ok(authResponseDto);
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        string email = registerDto.Email.ToLower();
        string username = registerDto.Username.ToLower();

        if (await _appDbContext.Users.AnyAsync(user => user.Email == email || user.Username == username))
        {
            return Result<AuthResponseDto>.Fail("User with this email or username already exists", ErrorKind.Conflict);
        }

        User user = new User
        {
            Email = email,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        };

        await _appDbContext.Users.AddAsync(user);
        await _appDbContext.SaveChangesAsync();

        string refreshToken = GenerateRefreshToken();

        RefreshToken refreshTokenEntity = new RefreshToken
        {
            Token = HashToken(refreshToken),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        await _appDbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _appDbContext.SaveChangesAsync();

        AuthResponseDto authResponseDto = new AuthResponseDto
        {
            AccessToken = GenerateAuthJwt(user),
            RefreshToken = refreshToken,
            User = UserMappings.ToResponseDto(user)
        };

        return Result<AuthResponseDto>.Ok(authResponseDto);
    }

    public async Task<Result<AuthResponseDto>> Refresh(TokenRequestDto request)
    {
        string tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _appDbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == tokenHash);

        if (storedToken == null ||
            storedToken.IsRevoked ||
            storedToken.Expires < DateTime.UtcNow)
        {
            return Result<AuthResponseDto>.Fail("Invalid refresh token", ErrorKind.Unauthorized);
        }

        // Presenting an already-used token means it may have been stolen:
        // revoke every active token for this user (reuse detection).
        if (storedToken.IsUsed)
        {
            await _appDbContext.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId && !rt.IsRevoked)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true));

            return Result<AuthResponseDto>.Fail("Invalid refresh token", ErrorKind.Unauthorized);
        }

        storedToken.IsUsed = true;

        User user = storedToken.User;

        string newAccessToken = GenerateAuthJwt(user);
        string newRefreshToken = GenerateRefreshToken();

        var newRefreshEntity = new RefreshToken
        {
            Token = HashToken(newRefreshToken),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _appDbContext.RefreshTokens.Add(newRefreshEntity);
        await _appDbContext.SaveChangesAsync();

        AuthResponseDto authResponseDto = new AuthResponseDto
        {
            User = UserMappings.ToResponseDto(user),
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return Result<AuthResponseDto>.Ok(authResponseDto);
    }

    public async Task<Result<bool>> LogoutAsync(int userId)
    {
        User? user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<bool>.Fail("User not found", ErrorKind.Unauthorized);
        }

        List<RefreshToken> refreshTokenList = await _appDbContext.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();

        foreach (RefreshToken refreshToken in refreshTokenList)
        {
            refreshToken.IsRevoked = true;
        }

        await _appDbContext.SaveChangesAsync();

        return Result<bool>.Ok(true);
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

    private string GenerateRefreshToken()
    {
        byte[] bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}