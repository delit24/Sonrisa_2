namespace AlertSystem.Api.Models.DTOs.Preferences;

public class PreferenceDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
