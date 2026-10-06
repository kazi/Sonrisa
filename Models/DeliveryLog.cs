namespace Sonrisa.Models;

public class DeliveryLog
{
    public int Id { get; set; }
    public string ChannelType { get; set; } = string.Empty; // "Slack" or "Email"
    public string Recipient { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
    public string Status { get; set; } = string.Empty; // "SUCCESS" or "FAILED"
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}