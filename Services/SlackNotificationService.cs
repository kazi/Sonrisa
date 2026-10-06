using System.Text;
using System.Text.Json;
using Sonrisa.Models;

namespace Sonrisa.Services;

public class SlackNotificationService
{
    private readonly HttpClient _httpClient;

    public SlackNotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendNewsNotificationAsync(string webhookUrl, List<NewsArticle> articles)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl) || !articles.Any())
            return false;

        // Build Slack Payload using Slack Block Kit JSON format
        var blocks = new List<object>
        {
            new
            {
                type = "header",
                text = new
                {
                    type = "plain_text",
                    text = "🔔 Sonrisa Breaking News Alert",
                    emoji = true
                }
            },
            new { type = "divider" }
        };

        foreach (var article in articles.Take(5)) // Top 5 headlines
        {
            var articleText = $"*<{article.Url}|{article.Title}>*\n" +
                              $"_Source: {article.SourceName ?? "Unknown"}_ | Published: {article.PublishedAt?.ToString("g")}\n" +
                              $"{(string.IsNullOrWhiteSpace(article.Description) ? "" : article.Description)}";

            blocks.Add(new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = articleText
                }
            });

            blocks.Add(new { type = "divider" });
        }

        var payload = new { blocks };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Slack Dispatch Error] {ex.Message}");
            return false;
        }
    }
}