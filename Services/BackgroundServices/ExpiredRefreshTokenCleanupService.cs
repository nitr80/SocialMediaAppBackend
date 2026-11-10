using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialMediaAppBackend.Services.Interfaces;

namespace SocialMediaAppBackend.Services.BackgroundServices;

public class ExpiredRefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredRefreshTokenCleanupService> _logger;

    public ExpiredRefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredRefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await CleanupExpiredTokensAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            IRefreshTokenCleanupService cleanupService =
                scope.ServiceProvider.GetRequiredService<IRefreshTokenCleanupService>();

            int deletedCount = await cleanupService.DeleteExpiredTokensAsync(stoppingToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Deleted {DeletedCount} expired refresh tokens.",
                    deletedCount);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down; stop without logging an error.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expired refresh tokens.");
        }
    }
}
