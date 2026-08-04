using SocialMediaAppBackend.DTOs.User;
using SocialMediaAppBackend.Models;

namespace SocialMediaAppBackend.Mappings;

public static class UserMappings
{
    public static UserResponseDto ToResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl  
        };
    }

    public static string ToBioString(UserBioRequestDto userBioRequestDto)
    {
        return userBioRequestDto.Bio;
    }
}