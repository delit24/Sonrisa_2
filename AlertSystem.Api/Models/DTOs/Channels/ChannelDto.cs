namespace AlertSystem.Api.Models.DTOs.Channels;

public class ChannelDto
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
