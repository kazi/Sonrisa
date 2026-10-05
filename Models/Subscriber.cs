namespace Sonrisa.Models;

public class Subscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool NotifyViaEmail { get; set; }
    public bool NotifyViaSlack { get; set; }
    public string? SlackWebhookUrl { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}