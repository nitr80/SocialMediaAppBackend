namespace SocialMediaAppBackend.DTOs.User;

using System.ComponentModel.DataAnnotations;

public class UserBioRequestDto
{
    [Required]
    public required string Bio { get; set; }
}