using Microsoft.EntityFrameworkCore;
using AlertSystem.Api.Data;
using AlertSystem.Api.Models.DTOs.Preferences;
using AlertSystem.Api.Models.Entities;

namespace AlertSystem.Api.Services;

public class PreferenceService
{
    private readonly AppDbContext _context;

    public PreferenceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PreferenceDto>> GetByUserAsync(int userId)
    {
        return await _context.AlertPreferences
            .Where(p => p.UserId == userId)
            .Select(p => new PreferenceDto
            {
                Id = p.Id,
                Category = p.Category,
                Keyword = p.Keyword,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(PreferenceDto? Preference, string? Error)> CreateAsync(int userId, CreatePreferenceRequest request)
    {
        // Normalize keyword: empty string becomes null
        var keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();

        // Check for duplicate
        var exists = await _context.AlertPreferences
            .AnyAsync(p => p.UserId == userId
                        && p.Category == request.Category
                        && p.Keyword == keyword);

        if (exists)
        {
            return (null, "Preference with this category and keyword already exists");
        }

        var preference = new AlertPreference
        {
            UserId = userId,
            Category = request.Category,
            Keyword = keyword,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AlertPreferences.Add(preference);
        await _context.SaveChangesAsync();

        return (new PreferenceDto
        {
            Id = preference.Id,
            Category = preference.Category,
            Keyword = preference.Keyword,
            IsActive = preference.IsActive,
            CreatedAt = preference.CreatedAt
        }, null);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var preference = await _context.AlertPreferences
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (preference == null) return false;

        _context.AlertPreferences.Remove(preference);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PreferenceDto?> ToggleAsync(int id, int userId)
    {
        var preference = await _context.AlertPreferences
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (preference == null) return null;

        preference.IsActive = !preference.IsActive;
        preference.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new PreferenceDto
        {
            Id = preference.Id,
            Category = preference.Category,
            Keyword = preference.Keyword,
            IsActive = preference.IsActive,
            CreatedAt = preference.CreatedAt
        };
    }
}
