namespace AlertSystem.Api.Models.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ChannelCount { get; set; }
    public int PreferenceCount { get; set; }
}
