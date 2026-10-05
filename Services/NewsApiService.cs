using Microsoft.EntityFrameworkCore;
using Sonrisa.Data;
using Sonrisa.Models;
using System.Net.Http.Json;

namespace Sonrisa.Services;

public class NewsApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public NewsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _httpClient = httpClient;
        _apiKey = configuration["NewsApi:ApiKey"]
            ?? throw new ArgumentNullException("NewsApi:ApiKey is not configured.");
        _dbContextFactory = dbContextFactory;
    }

    public async Task SyncAndGetHeadlinesAsync(string country = "us")
    {
        var requestUrl = $"v2/top-headlines?country={country}&apiKey={_apiKey}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<NewsApiResponseDto>(requestUrl);
            var apiArticles = response?.Articles ?? new List<NewsArticleDto>();

            if (apiArticles.Any())
            {
                await SaveNewArticlesAsync(apiArticles);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching news from API: {ex.Message}");
        }
    }

    private async Task SaveNewArticlesAsync(List<NewsArticleDto> apiArticles)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Extract incoming URLs (ignoring articles missing URLs)
        var incomingUrls = apiArticles
            .Where(a => !string.IsNullOrEmpty(a.Url))
            .Select(a => a.Url!)
            .ToList();

        // Query existing URLs (including soft-deleted ones using IgnoreQueryFilters)
        var existingUrls = await dbContext.NewsArticles
            .IgnoreQueryFilters()
            .Where(a => a.Url != null && incomingUrls.Contains(a.Url))
            .Select(a => a.Url!)
            .ToListAsync();

        // Filter out articles already present in the database
        var newArticles = apiArticles
            .Where(a => !string.IsNullOrEmpty(a.Url) && !existingUrls.Contains(a.Url))
            .Select(a => new NewsArticle
            {
                Title = a.Title,
                Description = a.Description,
                Url = a.Url,
                UrlToImage = a.UrlToImage,
                SourceName = a.Source?.Name,
                PublishedAt = a.PublishedAt,
                ImportedAt = DateTime.UtcNow,
                IsDeleted = false
            })
            .ToList();

        if (newArticles.Any())
        {
            dbContext.NewsArticles.AddRange(newArticles);
            await dbContext.SaveChangesAsync();
        }
    }

    // Get all active (non-deleted) articles from local DB
    public async Task<List<NewsArticle>> GetLocalArticlesAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.NewsArticles
            .OrderByDescending(a => a.PublishedAt ?? a.ImportedAt)
            .ToListAsync();
    }

    // Soft delete an article by ID
    public async Task SoftDeleteArticleAsync(int id)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var article = await dbContext.NewsArticles.FindAsync(id);
        if (article != null)
        {
            article.IsDeleted = true;
            article.DeletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}

// API Response DTOs
public class NewsApiResponseDto
{
    public List<NewsArticleDto> Articles { get; set; } = new();
}

public class NewsArticleDto
{
    public NewsSourceDto? Source { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? UrlToImage { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class NewsSourceDto
{
    public string? Name { get; set; }
}