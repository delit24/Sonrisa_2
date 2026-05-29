using Microsoft.EntityFrameworkCore;
using AlertSystem.Api.Data;
using AlertSystem.Api.Models.DTOs.Channels;
using AlertSystem.Api.Models.Entities;

namespace AlertSystem.Api.Services;

public class ChannelService
{
    private readonly AppDbContext _context;

    public ChannelService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChannelDto>> GetByUserAsync(int userId)
    {
        return await _context.NotificationChannels
            .Where(c => c.UserId == userId)
            .Select(c => new ChannelDto
            {
                Id = c.Id,
                Channel = c.Channel,
                Destination = c.Destination,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<ChannelDto> UpsertAsync(int userId, string channelType, UpsertChannelRequest request)
    {
        var channel = await _context.NotificationChannels
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Channel == channelType);

        if (channel == null)
        {
            channel = new NotificationChannel
            {
                UserId = userId,
                Channel = channelType,
                Destination = request.Destination,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.NotificationChannels.Add(channel);
        }
        else
        {
            channel.Destination = request.Destination;
        }

        await _context.SaveChangesAsync();

        return new ChannelDto
        {
            Id = channel.Id,
            Channel = channel.Channel,
            Destination = channel.Destination,
            IsActive = channel.IsActive
        };
    }

    public async Task<ChannelDto?> ToggleAsync(int id, int userId)
    {
        var channel = await _context.NotificationChannels
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (channel == null) return null;

        channel.IsActive = !channel.IsActive;
        await _context.SaveChangesAsync();

        return new ChannelDto
        {
            Id = channel.Id,
            Channel = channel.Channel,
            Destination = channel.Destination,
            IsActive = channel.IsActive
        };
    }
}
