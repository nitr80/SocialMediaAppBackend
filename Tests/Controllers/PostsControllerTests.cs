using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SocialMediaAppBackend.Controllers;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;
using SocialMediaAppBackend.Services.Interfaces;
using Xunit;

namespace SocialMediaAppBackend.Tests.Controllers;

public class PostsControllerTests
{
    private readonly Mock<IPostsService> _postsService = new();

    private static Post SamplePost()
    {
        return new Post
        {
            Id = 1,
            Content = "hello world",
            AuthorId = 42,
            Author = new User
            {
                Id = 42,
                Username = "alice",
                Email = "alice@example.com",
                PasswordHash = "hash"
            }
        };
    }

    private static void AssertMessageResult(ObjectResult result, int statusCode, string message)
    {
        Assert.Equal(statusCode, result.StatusCode);

        var payload = result.Value;
        Assert.NotNull(payload);
        var prop = payload!.GetType().GetProperty("message");
        Assert.NotNull(prop);
        Assert.Equal(message, prop!.GetValue(payload));
    }

    private static ControllerContext BuildControllerContextWithUserId(int userId)
    {
        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        userId.ToString())
                },
                "TestAuth"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    // ---------- GetAll ----------

    [Fact]
    public async Task GetAll_ReturnsOkWithPosts()
    {
        var posts = new List<PostResponseDto>
        {
            new()
            {
                Id = 1,
                Content = "hello",
                Author = new UserResponseDto { Id = 42, Username = "alice", Email = "alice@example.com" }
            }
        };

        _postsService
            .Setup(s => s.GetAllPosts(42))
            .ReturnsAsync(Result<List<PostResponseDto>>.Ok(posts));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(posts, ok.Value);
    }

    // ---------- GetById ----------

    [Fact]
    public async Task GetById_WhenPostExists_ReturnsOkWithMappedDto()
    {
        _postsService
            .Setup(s => s.GetPostById(1, 42))
            .ReturnsAsync(Result<Post>.Ok(SamplePost()));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PostResponseDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal("hello world", dto.Content);
        Assert.Equal("alice", dto.Author.Username);
    }

    [Fact]
    public async Task GetById_WhenPostMissing_ReturnsNotFoundWithMessage()
    {
        _postsService
            .Setup(s => s.GetPostById(999, 42))
            .ReturnsAsync(Result<Post>.Fail("Post not found", ErrorKind.NotFound));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetById(999);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        AssertMessageResult(objectResult, StatusCodes.Status404NotFound, "Post not found");
    }

    [Fact]
    public async Task GetById_WhenNotFollowing_ReturnsForbiddenWithMessage()
    {
        _postsService
            .Setup(s => s.GetPostById(1, 42))
            .ReturnsAsync(Result<Post>.Fail("Not authorized to view this post", ErrorKind.Forbidden));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.GetById(1);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        AssertMessageResult(objectResult, StatusCodes.Status403Forbidden, "Not authorized to view this post");
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtAction()
    {
        _postsService
            .Setup(s => s.CreatePost(It.IsAny<Post>()))
            .ReturnsAsync(Result<Post>.Ok(SamplePost()));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.Create(new PostRequestDto { Content = "hello world" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(PostsController.GetById), created.ActionName);

        var dto = Assert.IsType<PostResponseDto>(created.Value);
        Assert.Equal("hello world", dto.Content);

        // the author id came from the authenticated user's claim
        _postsService.Verify(s => s.CreatePost(It.Is<Post>(p => p.AuthorId == 42)), Times.Once);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsOkWithData()
    {
        _postsService
            .Setup(s => s.DeletePostById(1, 42))
            .ReturnsAsync(Result<bool>.Ok(true));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.Delete(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
    }

    [Fact]
    public async Task Delete_WhenNotOwner_ReturnsForbiddenWithMessage()
    {
        _postsService
            .Setup(s => s.DeletePostById(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<bool>.Fail("Not authorized", ErrorKind.Forbidden));

        var controller = new PostsController(_postsService.Object)
        {
            ControllerContext = BuildControllerContextWithUserId(42)
        };

        var result = await controller.Delete(1);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        AssertMessageResult(objectResult, StatusCodes.Status403Forbidden, "Not authorized");
    }
}
