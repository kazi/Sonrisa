using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Sonrisa.Models;
using Sonrisa.Models.Mailtrap;

namespace Sonrisa.Services;

public class EmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(HttpClient httpClient, IConfiguration config, ILogger<EmailNotificationService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendNewsNotificationAsync(string recipientEmail, List<NewsArticle> articles)
    {
        var apiToken = _config["Mailtrap:ApiToken"];
        var fromEmail = _config["Mailtrap:FromEmail"] ?? "no-reply@sonrisa.com";
        var fromName = _config["Mailtrap:FromName"] ?? "Sonrisa News Digest";

        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Mailtrap API token is not configured. Skipping email dispatch.");
            return false;
        }

        var payload = new MailtrapSendRequest
        {
            From = new MailtrapContact { Email = fromEmail, Name = fromName },
            To = new List<MailtrapContact> { new() { Email = recipientEmail } },
            Subject = $"Sonrisa Daily Digest - Top {articles.Count} Headlines",
            Html = BuildEmailHtmlBody(articles)
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://send.api.mailtrap.io/api/send");

            // Set Bearer token authorization header as required by Mailtrap API v2
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Mailtrap API v2 request failed with status {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Mailtrap API v2 to {RecipientEmail}", recipientEmail);
            return false;
        }
    }

    private static string BuildEmailHtmlBody(List<NewsArticle> articles)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; background-color: #f8f9fa; color: #333; padding: 20px; }
                    .container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; padding: 20px; border: 1px solid #e0e0e0; }
                    .header { text-align: center; border-bottom: 2px solid #0d6efd; padding-bottom: 10px; margin-bottom: 20px; }
                    .article { margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #eee; }
                    .article h3 { margin: 0 0 5px 0; font-size: 18px; }
                    .article a { color: #0d6efd; text-decoration: none; }
                    .article p { margin: 5px 0; color: #555; font-size: 14px; }
                    .meta { font-size: 12px; color: #888; }
                    .footer { text-align: center; font-size: 12px; color: #999; margin-top: 20px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h2>Sonrisa News Digest</h2>
                        <p>Your latest curated headlines</p>
                    </div>
            """);

        foreach (var article in articles)
        {
            sb.AppendFormat("""
                <div class="article">
                    <h3><a href="{0}" target="_blank">{1}</a></h3>
                    <p>{2}</p>
                    <div class="meta">Source: {3} | Published: {4}</div>
                </div>
                """,
                article.Url,
                WebUtility.HtmlEncode(article.Title),
                WebUtility.HtmlEncode(article.Description ?? "No description available."),
                WebUtility.HtmlEncode(article.SourceName ?? "Unknown"),
                article.PublishedAt?.ToString("MMM dd, yyyy HH:mm") ?? "N/A"
            );
        }

        sb.Append("""
                    <div class="footer">
                        <p>You are receiving this email because you subscribed to Sonrisa News updates.</p>
                    </div>
                </div>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}