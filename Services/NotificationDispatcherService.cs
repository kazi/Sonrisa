using Microsoft.EntityFrameworkCore;
using Sonrisa.Data;
using Sonrisa.Models;

namespace Sonrisa.Services;

public class NotificationDispatcherService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly SlackNotificationService _slackService;

    public NotificationDispatcherService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        SlackNotificationService slackService)
    {
        _dbContextFactory = dbContextFactory;
        _slackService = slackService;
    }

    public async Task<int> DispatchTopHeadlinesToSlackAsync(int topCount = 3)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();

        var recentArticles = await db.NewsArticles
            .OrderByDescending(a => a.PublishedAt ?? a.ImportedAt)
            .Take(topCount)
            .ToListAsync();

        if (!recentArticles.Any())
            return 0;

        var slackSubscribers = await db.Subscribers
            .Where(s => s.NotifyViaSlack && !string.IsNullOrEmpty(s.SlackWebhookUrl))
            .ToListAsync();

        int successCount = 0;

        foreach (var subscriber in slackSubscribers)
        {
            bool sent = await _slackService.SendNewsNotificationAsync(subscriber.SlackWebhookUrl!, recentArticles);

            var log = new DeliveryLog
            {
                ChannelType = "Slack",
                Recipient = subscriber.Email,
                ArticleCount = recentArticles.Count,
                Status = sent ? "SUCCESS" : "FAILED",
                ErrorMessage = sent ? null : "Failed to deliver payload to Slack webhook.",
                AttemptedAt = DateTime.UtcNow
            };

            db.DeliveryLogs.Add(log);

            if (sent)
            {
                successCount++;
            }
        }

        await db.SaveChangesAsync();
        return successCount;
    }
}