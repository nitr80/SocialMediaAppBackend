namespace SocialMediaAppBackend.Results;

public enum ErrorKind
{
    None,
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict
}
