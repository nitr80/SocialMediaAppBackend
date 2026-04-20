using SocialMediaAppBackend.DTOs.Post;
using SocialMediaAppBackend.Results;

namespace SocialMediaAppBackend.Services.Interfaces;

public interface IPostService
{
    public Task<Result<List<PostResponseDto>>> GetAllPosts();
    public Task<Result<PostResponseDto>> GetPostById(int id);
    public Task<Result<PostResponseDto>> CreatePost(PostRequestDto postRequestDto, int userId);
    public Task<Result<PostResponseDto>> DeletePostById(int id, int userId);
}