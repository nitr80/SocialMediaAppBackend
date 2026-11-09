using Microsoft.Data.Sqlite;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SocialMediaAppBackend.Data;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Services;

public class UsersServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        // SQLite in-memory needs the connection kept open for the DB's lifetime,
        // and it supports ExecuteDelete/ExecuteUpdate which the InMemory provider does not.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static UsersService CreateService(AppDbContext db, IBlobStorageService? blobStorage = null)
    {
        return new UsersService(db, blobStorage ?? new Mock<IBlobStorageService>().Object);
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

    private static IFormFile CreateFormFile(string fileName, int sizeInBytes = 16, byte[]? customBytes = null)
    {
        byte[] bytes;
        if (customBytes != null)
        {
            bytes = customBytes;
        }
        else
        {
            bytes = new byte[sizeInBytes];
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (sizeInBytes >= 8 && ext == ".png")
            {
                byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
                Array.Copy(pngHeader, bytes, pngHeader.Length);
            }
            else if (sizeInBytes >= 3 && (ext == ".jpg" || ext == ".jpeg"))
            {
                bytes[0] = 0xFF;
                bytes[1] = 0xD8;
                bytes[2] = 0xFF;
            }
            else if (sizeInBytes >= 12 && ext == ".webp")
            {
                bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
                bytes[8] = 0x57; bytes[9] = 0x45; bytes[10] = 0x42; bytes[11] = 0x50;
            }
        }

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

        var service = CreateService(db);

        var result = await service.GetUserById(user.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data!.Username);
    }

    [Fact]
    public async Task GetUserById_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

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

        var service = CreateService(db);

        var result = await service.AddOrUpdateBio("Hello there", user.Id);

        Assert.True(result.Success);

        var stored = await db.Users.SingleAsync();
        Assert.Equal("Hello there", stored.Bio);
    }

    [Fact]
    public async Task AddOrUpdateBio_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

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

        var mockBlobStorage = new Mock<IBlobStorageService>();
        mockBlobStorage
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), "avatar.png", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://pub-xxx.r2.dev/profiles/avatar-uuid.png");

        var service = CreateService(db, mockBlobStorage.Object);
        var image = CreateFormFile("avatar.png");

        var result = await service.AddOrUpdateProfilePicture(image, user.Id);

        Assert.True(result.Success);
        Assert.Equal("https://pub-xxx.r2.dev/profiles/avatar-uuid.png", result.Data);

        var stored = await db.Users.SingleAsync();
        Assert.Equal("https://pub-xxx.r2.dev/profiles/avatar-uuid.png", stored.ProfileImageUrl);
        mockBlobStorage.Verify(b => b.UploadAsync(It.IsAny<Stream>(), "avatar.png", "image/png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenUserHasOldAvatar_DeletesOldAvatar()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        user.ProfileImageUrl = "https://pub-xxx.r2.dev/profiles/old-avatar.png";
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockBlobStorage = new Mock<IBlobStorageService>();
        mockBlobStorage
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), "avatar.png", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://pub-xxx.r2.dev/profiles/new-avatar.png");

        var service = CreateService(db, mockBlobStorage.Object);
        var image = CreateFormFile("avatar.png");

        var result = await service.AddOrUpdateProfilePicture(image, user.Id);

        Assert.True(result.Success);
        Assert.Equal("https://pub-xxx.r2.dev/profiles/new-avatar.png", result.Data);

        var stored = await db.Users.SingleAsync();
        Assert.Equal("https://pub-xxx.r2.dev/profiles/new-avatar.png", stored.ProfileImageUrl);
        mockBlobStorage.Verify(b => b.DeleteAsync("https://pub-xxx.r2.dev/profiles/old-avatar.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WithInvalidMagicBytes_ReturnsValidationError()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        // Create a file with .png extension but with random non-PNG bytes
        var fakePng = CreateFormFile("avatar.png", customBytes: new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A });

        var result = await service.AddOrUpdateProfilePicture(fakePng, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
        Assert.Equal("The file content does not match a valid image format", result.Error);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WithValidJpegImage_StoresImageAndUpdatesUrl()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockBlobStorage = new Mock<IBlobStorageService>();
        mockBlobStorage
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), "avatar.jpg", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://pub-xxx.r2.dev/profiles/avatar.jpg");

        var service = CreateService(db, mockBlobStorage.Object);
        var image = CreateFormFile("avatar.jpg");

        var result = await service.AddOrUpdateProfilePicture(image, user.Id);

        Assert.True(result.Success);
        Assert.Equal("https://pub-xxx.r2.dev/profiles/avatar.jpg", result.Data);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WithValidWebpImage_StoresImageAndUpdatesUrl()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockBlobStorage = new Mock<IBlobStorageService>();
        mockBlobStorage
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), "avatar.webp", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://pub-xxx.r2.dev/profiles/avatar.webp");

        var service = CreateService(db, mockBlobStorage.Object);
        var image = CreateFormFile("avatar.webp");

        var result = await service.AddOrUpdateProfilePicture(image, user.Id);

        Assert.True(result.Success);
        Assert.Equal("https://pub-xxx.r2.dev/profiles/avatar.webp", result.Data);
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

        var service = CreateService(db);

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

        var service = CreateService(db);
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

        var service = CreateService(db);
        var empty = CreateFormFile("avatar.png", sizeInBytes: 0);

        var result = await service.AddOrUpdateProfilePicture(empty, user.Id);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.Validation, result.Kind);
    }

    [Fact]
    public async Task AddOrUpdateProfilePicture_WhenUserMissing_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.AddOrUpdateProfilePicture(CreateFormFile("avatar.png"), 999);

        Assert.False(result.Success);
        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }


    // --------- SearchUsername ----------

    [Fact]
    public async Task SearchUsername_WhenSearchTermProvided_ReturnsMatchingUsers()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        var currentUser = CreateUser("bob");
        db.Users.Add(currentUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SearchUsername("ali", currentUser.Id);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task SearchUsername_WhenNoSearchTermProvided_ReturnsAllUsers()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        var currentUser = CreateUser("bob");
        db.Users.Add(currentUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SearchUsername(null, currentUser.Id);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task SearchUsername_WhenNoMatches_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var user = CreateUser("alice");
        db.Users.Add(user);
        var currentUser = CreateUser("bob");
        db.Users.Add(currentUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SearchUsername("charlie", currentUser.Id);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }
}
