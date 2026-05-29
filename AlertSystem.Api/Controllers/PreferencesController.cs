using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Api.Models.DTOs.Preferences;
using AlertSystem.Api.Services;

namespace AlertSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly PreferenceService _preferenceService;

    public PreferencesController(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var preferences = await _preferenceService.GetByUserAsync(GetUserId());
        return Ok(preferences);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePreferenceRequest request)
    {
        var (preference, error) = await _preferenceService.CreateAsync(GetUserId(), request);

        if (error != null)
        {
            return Conflict(new { error, code = "DUPLICATE_PREFERENCE" });
        }

        return Created($"/api/preferences/{preference!.Id}", preference);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _preferenceService.DeleteAsync(id, GetUserId());
        if (!result)
        {
            return NotFound(new { error = "Preference not found", code = "NOT_FOUND" });
        }
        return NoContent();
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var preference = await _preferenceService.ToggleAsync(id, GetUserId());
        if (preference == null)
        {
            return NotFound(new { error = "Preference not found", code = "NOT_FOUND" });
        }
        return Ok(preference);
    }
}
