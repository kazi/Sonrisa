using Microsoft.EntityFrameworkCore;
using Sonrisa.Models;

namespace Sonrisa.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Ensure URL is unique so we don't import duplicates
        modelBuilder.Entity<NewsArticle>()
            .HasIndex(a => a.Url)
            .IsUnique();

        // 2. Global Query Filter: Automatically exclude soft-deleted items from normal queries
        modelBuilder.Entity<NewsArticle>()
            .HasQueryFilter(a => !a.IsDeleted);

        // Subscriber Configuration
        modelBuilder.Entity<Subscriber>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Subscriber>()
            .HasQueryFilter(s => !s.IsDeleted);
    }
}