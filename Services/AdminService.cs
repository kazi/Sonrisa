using Microsoft.EntityFrameworkCore;
using Sonrisa.Data;
using Sonrisa.Models;

namespace Sonrisa.Services;

public class AdminService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<AdminMetrics> GetMetricsAsync()
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();

        return new AdminMetrics
        {
            TotalActiveSubscribers = await db.Subscribers.CountAsync(),
            SlackSubscribersCount = await db.Subscribers.CountAsync(s => s.NotifyViaSlack),
            EmailSubscribersCount = await db.Subscribers.CountAsync(s => s.NotifyViaEmail),
            TotalDispatches = await db.DeliveryLogs.CountAsync(),
            SuccessfulDispatches = await db.DeliveryLogs.CountAsync(l => l.Status == "SUCCESS"),
            FailedDispatches = await db.DeliveryLogs.CountAsync(l => l.Status == "FAILED")
        };
    }

    public async Task<List<Subscriber>> GetAllSubscribersAsync(bool includeDeleted = false)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var query = db.Subscribers.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<List<DeliveryLog>> GetRecentDeliveryLogsAsync(int take = 50)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.DeliveryLogs
            .OrderByDescending(l => l.AttemptedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task ToggleSubscriberSoftDeleteAsync(int id)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var sub = await db.Subscribers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);

        if (sub != null)
        {
            sub.IsDeleted = !sub.IsDeleted;
            sub.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}

public class AdminMetrics
{
    public int TotalActiveSubscribers { get; set; }
    public int SlackSubscribersCount { get; set; }
    public int EmailSubscribersCount { get; set; }
    public int TotalDispatches { get; set; }
    public int SuccessfulDispatches { get; set; }
    public int FailedDispatches { get; set; }
}