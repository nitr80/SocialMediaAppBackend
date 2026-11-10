namespace SocialMediaAppBackend.Services.Interfaces;

public interface IRefreshTokenCleanupService
{
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}
