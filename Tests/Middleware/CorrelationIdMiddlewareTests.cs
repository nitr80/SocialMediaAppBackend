using Microsoft.AspNetCore.Http;
using SocialMediaAppBackend.Middleware;
using Xunit;

namespace SocialMediaAppBackend.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidHeader_UsesHeaderAndSetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "client-correlation-id";
        bool nextCalled = false;

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("client-correlation-id", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("client-correlation-id", context.Items[CorrelationIdMiddleware.ItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        string? correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName];
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
    }

    [Theory]
    [InlineData("bad id with spaces")]
    [InlineData("bad\nid")]
    [InlineData("")]
    public async Task InvokeAsync_WithInvalidHeader_GeneratesNewCorrelationId(string invalid)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = invalid;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        string? correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName];
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual(invalid, correlationId);
        Assert.Matches("^[A-Za-z0-9._-]+$", correlationId!);
    }

    [Fact]
    public async Task InvokeAsync_WithTooLongHeader_GeneratesNewCorrelationId()
    {
        string invalid = new string('a', 129);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = invalid;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        string? correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName];
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual(invalid, correlationId);
    }
}
