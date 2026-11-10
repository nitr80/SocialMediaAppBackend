using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services;

public class RefreshTokenCleanupService : IRefreshTokenCleanupService
{
    private readonly AppDbContext _appDbContext;

    public RefreshTokenCleanupService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.RefreshTokens
            .Where(rt => rt.Expires < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
