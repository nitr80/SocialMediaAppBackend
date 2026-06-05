using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        string? value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(value, out int userId))
        {
            throw new Exception("Invalid token user id");
        }

        return userId;
    }
}