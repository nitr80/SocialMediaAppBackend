using Microsoft.AspNetCore.Http;
using SocialMediaAppBackend.Middleware;
using Xunit;

namespace SocialMediaAppBackend.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SetsExpectedSecurityHeaders_AndCallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SecurityHeadersMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("camera=(), microphone=(), geolocation=()", context.Response.Headers["Permissions-Policy"]);
    }
}
