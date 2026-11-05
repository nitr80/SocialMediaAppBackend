using System.Text;
using System.Text.Json;

namespace SocialMediaAppBackend.Models;

public record PostCursor(DateTime CreatedAt, int Id)
{
    public static PostCursor DecodePostCursor(string cursorString)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursorString));

        return JsonSerializer.Deserialize<PostCursor>(json) ?? throw new ArgumentException("Invalid cursor.");
    }

    public static string EncodePostCursor(PostCursor postCursor)
    {
        var json = JsonSerializer.Serialize(postCursor);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}