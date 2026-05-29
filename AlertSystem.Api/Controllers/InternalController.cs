using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Api.Data;
using AlertSystem.Api.Models.Internal;

namespace AlertSystem.Api.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalController : ControllerBase
{
    private readonly AppDbContext _context;

    public InternalController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all active user preferences and channels for n8n workflow.
    /// Protected by API key middleware, not JWT.
    /// </summary>
    [HttpGet("user-preferences")]
    public async Task<IActionResult> GetUserPreferences()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Include(u => u.AlertPreferences.Where(p => p.IsActive))
            .Include(u => u.NotificationChannels)
            .ToListAsync();

        var response = users
            .Where(u => u.AlertPreferences.Count > 0)
            .Select(u => new UserPreferencesResponse
            {
                UserId = u.Id,
                Preferences = u.AlertPreferences.Select(p => new PreferenceItem
                {
                    Category = p.Category,
                    Keyword = p.Keyword
                }).ToList(),
                Channels = u.NotificationChannels.Select(c => new ChannelItem
                {
                    Type = c.Channel,
                    Destination = c.Destination,
                    IsActive = c.IsActive
                }).ToList()
            })
            .ToList();

        return Ok(response);
    }
}
