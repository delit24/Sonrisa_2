using System.ComponentModel.DataAnnotations;

namespace AlertSystem.Api.Models.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MinLength(6)]
    public string? Password { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(admin|user)$", ErrorMessage = "Role must be 'admin' or 'user'")]
    public string Role { get; set; } = "user";
}
