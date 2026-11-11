using OpenTelemetry.Metrics;

namespace SocialMediaAppBackend.Extensions;

public static class MetricsExtensions
{
    public static WebApplicationBuilder AddObservabilityMetrics(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddView(
                        "http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[]
                            {
                                0.005, 0.01, 0.025, 0.05, 0.075, 0.1,
                                0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10
                            }
                        })
                    .AddPrometheusExporter();
            });

        return builder;
    }

    public static WebApplication MapObservabilityEndpoints(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint()
            .DisableHttpMetrics();

        return app;
    }
}
