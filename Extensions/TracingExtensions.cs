using System.Reflection;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SocialMediaAppBackend.Extensions;

public static class TracingExtensions
{
    public static WebApplicationBuilder AddDistributedTracing(this WebApplicationBuilder builder)
    {
        string serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "SocialMediaAppBackend";
        string? otlpEndpoint = GetOtlpEndpoint(builder.Configuration);

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    serviceInstanceId: Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddNpgsql();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
                else if (builder.Environment.IsDevelopment())
                {
                    tracing.AddConsoleExporter();
                }
            });

        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        if (string.IsNullOrWhiteSpace(GetOtlpEndpoint(app.Configuration)) &&
            !app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(
                "OpenTelemetry tracing is enabled but no exporter is configured. " +
                "Set OpenTelemetry:OtlpEndpoint or OTEL_EXPORTER_OTLP_ENDPOINT to export traces.");
        }

        return app;
    }

    private static string? GetOtlpEndpoint(IConfiguration configuration)
    {
        string? endpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        }

        return endpoint;
    }
}
