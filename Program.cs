using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sonrisa.Components;
using Sonrisa.Data;
using Sonrisa.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Sonrisa.AdminAuth";
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState(); // Exposes AuthenticationState to Blazor components

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
builder.Services.AddHttpClient<EmailNotificationService>();
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

app.UseAuthentication();
app.UseAuthorization();

// 3. HTTP Endpoints for Issuing and Revoking Auth Cookies
app.MapPost("/api/admin/login", async (
    HttpContext httpContext,
    [FromForm] string password,
    [FromForm] string? returnUrl) =>
{
    // Retrieve admin password from appsettings.json or configuration
    var configuredPassword = app.Configuration["Admin:Password"] ?? "SonrisaAdmin123!";

    if (password == configuredPassword)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "AdminUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await httpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl);
    }

    return Results.Redirect("/admin/login?error=invalid_credentials");
}).DisableAntiforgery();

app.MapGet("/api/admin/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/admin/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
