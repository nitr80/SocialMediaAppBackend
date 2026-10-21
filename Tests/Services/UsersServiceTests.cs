using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class UsersServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        // SQLite in-memory needs the connection kept open for the DB's lifetime.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static User CreateUser(string username = "alice")
    {
        return new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hash"
        };
    }

    private static IFormFile CreateFormFile(string fileName, int sizeInBytes = 16)
    {
        var bytes = new byte[sizeInBytes];
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    // ---------- GetUserById ----------

    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsUser()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UsersService(db);

        var result = await service.GetUserById(user.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data!.Username);
    }

    [Fact]
    public async Task GetUserById_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = new UsersService(db);

        var result = await service.GetUserById(999);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    // ---------- AddOrUpdateBio ----------

    [Fact]
    public async Task AddOrUpdateBio_WhenUserExists_PersistsBio()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UsersService(db);

        var result = await service.AddOrUpdateBio("Hello there", user.Id);

        Assert.True(result.Success);

        var stored = await db.Users.SingleAsync();
        Assert.Equal("Hello there", stored.Bio);
    }

    [Fact]
    public async Task AddOrUpdateBio_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = new UsersService(db);

        var result = await service.AddOrUpdateBio("Hello", 999);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    // ---------- AddOrUpdateProfilePicture ----------

    [Fact]
    public async Task AddOrUpdateProfilePicture_WithValidImage_StoresImageAndUpdatesUrl()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // The service writes into wwwroot/images/profiles, which normally exists in the app.
        var profilesFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "images",
            "profiles");
        Directory.CreateDirectory(profilesFolder);

        var service = new UsersService(db);
        var image = CreateFormFile("avatar.png");

        var result = await service.AddOrUpdateProfilePicture(image, user.Id);

        Assert.True(result.Success);

        var stored = await db.Users.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(stored.ProfileImageUrl));
        Assert.StartsWith("/images/profiles/", stored.ProfileImageUrl);
        Assert.EndsWith(".png", stored.ProfileImageUrl);

        var savedPath = Path.Combine(profilesFolder, Path.GetFileName(stored.ProfileImageUrl!));
        Assert.True(File.Exists(savedPath));

        File.Delete(savedPath);
    }

    [Theory]
    [InlineData("avatar.gif")]
    [InlineData("avatar.exe")]
    [InlineData("avatar")]
    public async Task AddOrUpdateProfilePicture_WithDisallowedExtension_ReturnsValidationError(string fileName)
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UsersService(db);

        var result = await service.AddOrUpdateProfilePicture(CreateFormFile(fileName), user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenTooLarge_ReturnsValidationError()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UsersService(db);
        var tooLarge = CreateFormFile("avatar.png", sizeInBytes: 5 * 1024 * 1024 + 1);

        var result = await service.AddOrUpdateProfilePicture(tooLarge, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WithEmptyFile_ReturnsValidationError()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UsersService(db);
        var empty = CreateFormFile("avatar.png", sizeInBytes: 0);

        var result = await service.AddOrUpdateProfilePicture(empty, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = new UsersService(db);

        var result = await service.AddOrUpdateProfilePicture(CreateFormFile("avatar.png"), 999);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }
}
