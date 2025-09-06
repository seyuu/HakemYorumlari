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

// Mevcut servis kayıtlarına ekle
builder.Services.AddScoped<AIVideoAnalysisService>();

// WebApplication'ı oluştur
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
