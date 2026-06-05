using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Models;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IPostsService
{
    public Task<Result<List<Post>>> GetAllPosts();
    public Task<Result<Post>> GetPostById(int postId);
    public Task<Result<Post>> CreatePost(Post post);
    public Task<Result<bool>> DeletePostById(int postId, int userId);
}