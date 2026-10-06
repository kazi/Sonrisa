using Microsoft.EntityFrameworkCore;
using Sonrisa.Data;
using Sonrisa.Models;

namespace Sonrisa.Services;

public class NotificationDispatcherService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly SlackNotificationService _slackService;
    private readonly EmailNotificationService _emailService;

    public NotificationDispatcherService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        SlackNotificationService slackService,
        EmailNotificationService emailService)
    {
        _dbContextFactory = dbContextFactory;
        _slackService = slackService;
        _emailService = emailService;
    }

    /// <summary>
    /// Dispatches top headlines to all eligible active subscribers across Slack and Email channels.
    /// Returns total successful dispatches.
    /// </summary>
    public async Task<int> DispatchTopHeadlinesAsync(int topCount = 3)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();

        var recentArticles = await db.NewsArticles
            .OrderByDescending(a => a.PublishedAt ?? a.ImportedAt)
            .Take(topCount)
            .ToListAsync();

        if (!recentArticles.Any())
            return 0;

        int totalSuccesses = 0;

        // 1. Process Slack Subscriptions
        var slackSubscribers = await db.Subscribers
            .Where(s => s.NotifyViaSlack && !string.IsNullOrEmpty(s.SlackWebhookUrl))
            .ToListAsync();

        foreach (var subscriber in slackSubscribers)
        {
            bool sent = await _slackService.SendNewsNotificationAsync(subscriber.SlackWebhookUrl!, recentArticles);

            db.DeliveryLogs.Add(new DeliveryLog
            {
                ChannelType = "Slack",
                Recipient = subscriber.Email,
                ArticleCount = recentArticles.Count,
                Status = sent ? "SUCCESS" : "FAILED",
                ErrorMessage = sent ? null : "Failed to deliver payload to Slack webhook.",
                AttemptedAt = DateTime.UtcNow
            });

            if (sent) totalSuccesses++;
        }

        // 2. Process Email Subscriptions
        var emailSubscribers = await db.Subscribers
            .Where(s => s.NotifyViaEmail)
            .ToListAsync();

        foreach (var subscriber in emailSubscribers)
        {
            bool sent = await _emailService.SendNewsNotificationAsync(subscriber.Email, recentArticles);

            db.DeliveryLogs.Add(new DeliveryLog
            {
                ChannelType = "Email",
                Recipient = subscriber.Email,
                ArticleCount = recentArticles.Count,
                Status = sent ? "SUCCESS" : "FAILED",
                ErrorMessage = sent ? null : "Failed to send email via SMTP.",
                AttemptedAt = DateTime.UtcNow
            });

            if (sent) totalSuccesses++;
        }

        await db.SaveChangesAsync();
        return totalSuccesses;
    }
}