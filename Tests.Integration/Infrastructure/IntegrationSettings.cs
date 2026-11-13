namespace SocialMediaAppBackend.Tests.Integration.Infrastructure;

/// <summary>
/// Controls whether the integration tests (which require a Docker daemon) run.
/// They are opt-in so that a plain <c>dotnet test</c> stays Docker-free.
/// </summary>
public static class IntegrationSettings
{
    public const string SkipReason =
        "Integration tests are disabled. Set RUN_INTEGRATION_TESTS=true and ensure Docker is running.";

    public static bool Enabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
