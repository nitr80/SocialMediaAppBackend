using System.Collections;

namespace SocialMediaAppBackend.DTOs;

public class PagedList<T>
{
    public List<T> Values = new List<T>();
    public string? Cursor;
}