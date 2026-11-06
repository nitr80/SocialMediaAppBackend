using System.Collections;

namespace SocialMediaAppBackend.DTOs;

public class PagedList<T>
{
    public List<T> Values { get; set; } = new();
    public string? Cursor { get; set; }
}