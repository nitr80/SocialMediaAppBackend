using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Tests.Integration.Infrastructure;
using Xunit;

namespace SocialMediaAppBackend.Tests.Integration;

/// <summary>
/// Verifies the Postgres-specific pieces of the schema that SQLite cannot model:
/// the pg_trgm extension, the GIN trigram index and the feed index. These only
/// exist if the real migrations ran against a real PostgreSQL server.
/// </summary>
public sealed class DatabaseSchemaTests : IntegrationTestBase
{
    public DatabaseSchemaTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Migrations_EnablePgTrgmExtension()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        await using AppDbContext db = Fixture.CreateContext();

        int count = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM pg_extension WHERE extname = 'pg_trgm'")
            .SingleAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Migrations_CreateGinTrigramIndexOnUsername()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        await using AppDbContext db = Fixture.CreateContext();

        int count = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes " +
                "WHERE indexname = 'IX_Users_Username_Trgm' AND indexdef LIKE '%USING gin%'")
            .SingleAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Migrations_CreatePostsCreatedAtIdIndex()
    {
        Assert.SkipUnless(IntegrationSettings.Enabled, IntegrationSettings.SkipReason);

        await using AppDbContext db = Fixture.CreateContext();

        int count = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE indexname = 'IX_Posts_CreatedAt_Id'")
            .SingleAsync();

        Assert.Equal(1, count);
    }
}
