namespace AlertSystem.Api.Models.Internal;

public class UserPreferencesResponse
{
    public int UserId { get; set; }
    public List<PreferenceItem> Preferences { get; set; } = [];
    public List<ChannelItem> Channels { get; set; } = [];
}

public class PreferenceItem
{
    public string Category { get; set; } = string.Empty;
    public string? Keyword { get; set; }
}

public class ChannelItem
{
    public string Type { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
