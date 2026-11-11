using Serilog;
using Serilog.Events;
using SocialMediaAppBackend.Middleware;

namespace SocialMediaAppBackend.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });

        return builder;
    }

    public static WebApplication UseStructuredRequestLogging(this WebApplication app)
    {
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/health"),
            branch => branch.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

                options.GetLevel = (httpContext, _, exception) =>
                {
                    if (exception is not null ||
                        httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                    {
                        return LogEventLevel.Error;
                    }

                    if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
                    {
                        return LogEventLevel.Warning;
                    }

                    return LogEventLevel.Information;
                };

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set(
                        "CorrelationId",
                        httpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? string.Empty);
                    diagnosticContext.Set(
                        "ClientIp",
                        httpContext.Connection.RemoteIpAddress?.ToString());
                    diagnosticContext.Set(
                        "UserAgent",
                        httpContext.Request.Headers["User-Agent"].ToString());
                };
            }));

        return app;
    }
}
