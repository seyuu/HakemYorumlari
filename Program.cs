using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Host validation'ı tamamen devre dışı bırak
builder.WebHost.UseKestrel(options =>
{
    options.AllowSynchronousIO = true;
});

// AllowedHosts'u temizle
builder.Configuration["AllowedHosts"] = "*";

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient servisleri
builder.Services.AddHttpClient<HakemYorumlari.Services.HakemYorumuToplamaServisi>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<TVKanalScrapingService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", 
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Entity Framework - Production için özel yapılandırma
if (builder.Environment.IsProduction())
{
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("SQL_CONNECTION_STRING environment variable production ortamında zorunludur.");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Development modunda hem appsettings.json'dan hem environment variable'dan oku
    var connectionString = builder.Configuration.GetConnectionString("SQL_CONNECTION_STRING") 
                          ?? Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("SQL_CONNECTION_STRING connection string development ortamında zorunludur.");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Servisleri ekle
builder.Services.AddScoped<YouTubeScrapingService>();
builder.Services.AddScoped<TVKanalScrapingService>();
builder.Services.AddScoped<HakemYorumlari.Services.HakemYorumuToplamaServisi>();
builder.Services.AddScoped<BeINSportsEmbedService>();
builder.Services.AddScoped<SkorCekmeServisi>();
builder.Services.AddScoped<PozisyonOtomatikTespitServisi>();
builder.Services.AddScoped<FiksturGuncellemeServisi>();
builder.Services.AddHostedService<MacTakipBackgroundService>();

var app = builder.Build();

// Cloud Run için port yapılandırması - EN BAŞTA!
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Logger.LogInformation($"Uygulama {port} portunda başlatılıyor...");

// ÖNCE ForwardedHeaders - EN ÖNEMLİ!
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    RequireHeaderSymmetry = false,
    ForwardLimit = null
});

// Cloud Run için doğru port binding
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

// Request logging ekle
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"İstek alındı: {context.Request.Method} {context.Request.Path} - Host: {context.Request.Host}");
    await next();
});

// Cloud Run'da HTTPS redirection kullanma
// app.UseHttpsRedirection(); // Bu satırı kaldırdık

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
