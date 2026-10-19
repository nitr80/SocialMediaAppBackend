using System.ComponentModel.DataAnnotations;

namespace SocialMediaAppBackend.DTOs.User;

public class UserBioRequestDto
{
    [Required]
    [MaxLength(500)]
    public required string Bio { get; set; }
}