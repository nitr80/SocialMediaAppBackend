using System.Security.Claims;

namespace SocialMediaAppBackend.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        string? value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(value, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid token user id");
        }

        return userId;
    }
}