using System.ComponentModel.DataAnnotations;

namespace AlertSystem.Api.Models.DTOs.Channels;

public class UpsertChannelRequest
{
    [Required]
    public string Destination { get; set; } = string.Empty;
}
