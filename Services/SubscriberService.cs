using Microsoft.EntityFrameworkCore;
using Sonrisa.Data;
using Sonrisa.Models;

namespace Sonrisa.Services;

public class SubscriberService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public SubscriberService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Subscriber?> GetByEmailAsync(string email)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Subscribers.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task SaveSubscriptionAsync(string email, bool notifyViaEmail, bool notifyViaSlack, string? slackWebhookUrl = null)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var normalizedEmail = email.Trim().ToLower();

        // Check for existing record (including soft-deleted ones)
        var existing = await db.Subscribers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);

        // Rule: If both checkboxes are unchecked, remove (or soft-delete) the subscriber
        if (!notifyViaEmail && !notifyViaSlack)
        {
            if (existing != null && !existing.IsDeleted)
            {
                existing.IsDeleted = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return;
        }

        if (existing == null)
        {
            // Create new subscriber
            var subscriber = new Subscriber
            {
                Email = normalizedEmail,
                NotifyViaEmail = notifyViaEmail,
                NotifyViaSlack = notifyViaSlack,
                SlackWebhookUrl = slackWebhookUrl,
                CreatedAt = DateTime.UtcNow
            };
            db.Subscribers.Add(subscriber);
        }
        else
        {
            // Restore if previously deleted and update preferences
            existing.IsDeleted = false;
            existing.NotifyViaEmail = notifyViaEmail;
            existing.NotifyViaSlack = notifyViaSlack;
            if (!string.IsNullOrEmpty(slackWebhookUrl))
            {
                existing.SlackWebhookUrl = slackWebhookUrl;
            }
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<Subscriber>> GetActiveSubscribersAsync()
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Subscribers.ToListAsync();
    }
}