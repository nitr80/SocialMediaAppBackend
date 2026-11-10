using SocialMediaAppBackend.DTOs;
using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IPostsService
{
    public Task<Result<PagedList<PostResponseDto>>> GetAllPosts(int currentUserId, int? limit, string? cursor, CancellationToken cancellationToken = default);
    public Task<Result<Post>> GetPostById(int postId, int currentUserId, CancellationToken cancellationToken = default);
    public Task<Result<Post>> CreatePost(Post post, CancellationToken cancellationToken = default);
    public Task<Result<bool>> DeletePostById(int postId, int userId, CancellationToken cancellationToken = default);
}
