namespace Sonrisa.Models
{
    public class NewsArticle
    {
        public int Id { get; set; }
        public NewsSource? Source { get; set; }
        public string? Author { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? UrlToImage { get; set; }
        public string? SourceName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? Content { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}
