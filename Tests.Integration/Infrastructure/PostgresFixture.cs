using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration.Infrastructure;

/// <summary>
/// Starts a single ephemeral PostgreSQL container for the whole test collection
/// and applies the real EF Core migrations once. This is the same provider the
/// production app uses, so Postgres-specific behavior (pg_trgm, ILIKE, GIN
/// indexes, timestamptz) is exercised for real.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not running.");

    public async ValueTask InitializeAsync()
    {
        if (!IntegrationSettings.Enabled)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("socialmediaapp")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        await using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "integration";
}
