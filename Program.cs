using Microsoft.EntityFrameworkCore;
using Sonrisa.Components;
using Sonrisa.Data;
using Sonrisa.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=news.db"));

// Register HttpClient with NewsAPI Base URL
builder.Services.AddHttpClient<NewsApiService>(client =>
{
    client.BaseAddress = new Uri("https://newsapi.org/");
    // NewsAPI accepts key either via query string or via User-Agent header
    client.DefaultRequestHeaders.Add("User-Agent", "BlazorNewsApp/1.0");
});
builder.Services.AddHttpClient<SlackNotificationService>();
builder.Services.AddTransient<NotificationDispatcherService>();
builder.Services.AddTransient<SubscriberService>();
builder.Services.AddTransient<AdminService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
