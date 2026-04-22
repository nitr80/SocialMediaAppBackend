using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IPostsService
{
    public Task<Result<List<PostResponseDto>>> GetAllPosts();
    public Task<Result<PostResponseDto>> GetPostById(int postId);
    public Task<Result<PostResponseDto>> CreatePost(PostRequestDto postRequestDto, int userId);
    public Task<Result<bool>> DeletePostById(int postId, int userId);
}