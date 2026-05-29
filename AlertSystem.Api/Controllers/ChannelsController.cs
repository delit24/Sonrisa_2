using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Api.Models.DTOs.Channels;
using AlertSystem.Api.Services;

namespace AlertSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly ChannelService _channelService;

    public ChannelsController(ChannelService channelService)
    {
        _channelService = channelService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var channels = await _channelService.GetByUserAsync(GetUserId());
        return Ok(channels);
    }

    [HttpPut("email")]
    public async Task<IActionResult> UpsertEmail([FromBody] UpsertChannelRequest request)
    {
        var channel = await _channelService.UpsertAsync(GetUserId(), "email", request);
        return Ok(channel);
    }

    [HttpPut("slack")]
    public async Task<IActionResult> UpsertSlack([FromBody] UpsertChannelRequest request)
    {
        var channel = await _channelService.UpsertAsync(GetUserId(), "slack", request);
        return Ok(channel);
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var channel = await _channelService.ToggleAsync(id, GetUserId());
        if (channel == null)
        {
            return NotFound(new { error = "Channel not found", code = "NOT_FOUND" });
        }
        return Ok(channel);
    }
}
