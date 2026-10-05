namespace Sonrisa.Models
{
    public class NewsApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<NewsArticle> Articles { get; set; } = new();
    }
}
