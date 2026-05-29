using Microsoft.EntityFrameworkCore;
using AlertSystem.Api.Data;
using AlertSystem.Api.Models.DTOs.Users;
using AlertSystem.Api.Models.Entities;

namespace AlertSystem.Api.Services;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        return await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                ChannelCount = u.NotificationChannels.Count,
                PreferenceCount = u.AlertPreferences.Count
            })
            .ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                ChannelCount = u.NotificationChannels.Count,
                PreferenceCount = u.AlertPreferences.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(UserDto? User, string? Error)> CreateAsync(CreateUserRequest request)
    {
        // Check email uniqueness
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return (null, "A user with this email already exists");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            ChannelCount = 0,
            PreferenceCount = 0
        }, null);
    }

    public async Task<(UserDto? User, string? Error)> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return (null, "User not found");
        }

        // Check email uniqueness if changed
        if (user.Email != request.Email &&
            await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return (null, "A user with this email already exists");
        }

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        var channelCount = await _context.NotificationChannels.CountAsync(c => c.UserId == id);
        var prefCount = await _context.AlertPreferences.CountAsync(p => p.UserId == id);

        return (new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            ChannelCount = channelCount,
            PreferenceCount = prefCount
        }, null);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
