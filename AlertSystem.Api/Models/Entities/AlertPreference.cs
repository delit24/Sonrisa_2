namespace AlertSystem.Api.Models.Entities;

public class AlertPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    // "breaking_news" | "market" | "natural_disaster" | "tech"
    public string? Keyword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
