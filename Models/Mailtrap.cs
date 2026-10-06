using System.Text.Json.Serialization;

namespace Sonrisa.Models.Mailtrap;

public class MailtrapSendRequest
{
    [JsonPropertyName("from")]
    public MailtrapContact From { get; set; } = new();

    [JsonPropertyName("to")]
    public List<MailtrapContact> To { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "News Digest";
}

public class MailtrapContact
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}