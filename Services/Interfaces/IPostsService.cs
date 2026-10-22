using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IPostsService
{
    public Task<Result<List<PostResponseDto>>> GetAllPosts(int currentUserId);
    public Task<Result<Post>> GetPostById(int postId, int currentUserId);
    public Task<Result<Post>> CreatePost(Post post);
    public Task<Result<bool>> DeletePostById(int postId, int userId);
}