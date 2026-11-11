using System.Diagnostics;
using Serilog.Context;

namespace SocialMediaAppBackend.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private const int MaxLength = 128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrCreateCorrelationId(context);

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Activity? activity = Activity.Current;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (activity is null)
            {
                await _next(context);
                return;
            }

            using (LogContext.PushProperty("TraceId", activity.TraceId.ToString()))
            using (LogContext.PushProperty("SpanId", activity.SpanId.ToString()))
            {
                await _next(context);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values) &&
            values.Count == 1 &&
            IsValid(values[0]))
        {
            return values[0]!;
        }

        Activity? activity = Activity.Current;
        if (activity is not null && activity.TraceId != default)
        {
            return activity.TraceId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) &&
                character != '-' &&
                character != '_' &&
                character != '.')
            {
                return false;
            }
        }

        return true;
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
