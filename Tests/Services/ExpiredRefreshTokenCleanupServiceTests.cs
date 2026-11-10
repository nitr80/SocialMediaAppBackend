using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SocialMediaAppBackend.Services.BackgroundServices;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class ExpiredRefreshTokenCleanupServiceTests
{
    [Fact]
    public async Task StartAsync_RunsCleanupImmediately()
    {
        var cleanupServiceMock = new Mock<IRefreshTokenCleanupService>();
        var cleanupCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        cleanupServiceMock
            .Setup(s => s.DeleteExpiredTokensAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                cleanupCalled.TrySetResult(true);
                await Task.CompletedTask;
                return 0;
            });

        var services = new ServiceCollection();
        services.AddSingleton<IRefreshTokenCleanupService>(cleanupServiceMock.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var hostedService = new ExpiredRefreshTokenCleanupService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ExpiredRefreshTokenCleanupService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        await cleanupCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await hostedService.StopAsync(CancellationToken.None);

        cleanupServiceMock.Verify(
            s => s.DeleteExpiredTokensAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
