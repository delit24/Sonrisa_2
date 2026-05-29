namespace AlertSystem.Api.Models.Entities;

public class NotificationChannel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Channel { get; set; } = string.Empty;
    // "email" | "slack"
    public string Destination { get; set; } = string.Empty;
    // email cím VAGY Slack channel név (#alerts, @username)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
