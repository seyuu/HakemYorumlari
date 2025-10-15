using HakemYorumlari.Data;
using HakemYorumlari.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Google.Cloud.Storage.V1;

var builder = WebApplication.CreateBuilder(args);

// Logging yapılandırması
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    builder.Logging.AddFilter("System", LogLevel.Warning);
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data Protection yapılandırması
try
{
    var keyFilePath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    var bucketName = Environment.GetEnvironmentVariable("GCS_BUCKET_NAME") ?? "hakemyorumlari-keys";

    if (!string.IsNullOrEmpty(keyFilePath) && File.Exists(keyFilePath))
    {
        // Google Cloud entegrasyonu yoksa yerel dosya sistemi kullan
        builder.Services.AddDataProtection()
            .SetApplicationName("HakemYorumlari")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));

        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        logger?.LogInformation("Data Protection yerel dosya sistemine yapılandırıldı (Google Cloud bulunamadı).");
    }
    else
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("HakemYorumlari")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));

        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        logger?.LogWarning("Google Cloud Storage yapılandırılmadı, yerel Data Protection kullanılacak");
    }
}
catch (Exception ex)
{
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger?.LogWarning(ex, "Data Protection yapılandırılamadı, varsayılan ayarlar kullanılacak");

    builder.Services.AddDataProtection()
        .SetApplicationName("HakemYorumlari")
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));
}

// HttpClient servisleri
builder.Services.AddHttpClient<HakemYorumuToplamaServisi>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(60);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<TVKanalScrapingService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(45);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<YouTubeScrapingService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<BeINSportsEmbedService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<SkorCekmeServisi>().AddStandardResilienceHandler();
builder.Services.AddHttpClient("DefaultHttpClient").AddStandardResilienceHandler();

// Entity Framework - Production ve Development için ayrı yapılandırma
if (builder.Environment.IsProduction())
{
    // Production'da environment variable kullan
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("SQL_CONNECTION_STRING environment variable production ortamında zorunludur.");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));
    
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger?.LogInformation("Production database bağlantısı yapılandırıldı");
}
else
{
    // Development'ta appsettings.json'dan al
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        // Fallback connection string
        connectionString = "Server=localhost;Database=HakemYorumlari;Trusted_Connection=True;TrustServerCertificate=True;";
        
        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        logger?.LogWarning("DefaultConnection bulunamadı, fallback connection string kullanılıyor");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging() // Development'ta detaylı log
        .EnableDetailedErrors());     // Development'ta detaylı hatalar
}

// Servisleri ekle
builder.Services.AddScoped<YouTubeScrapingService>();
builder.Services.AddScoped<TVKanalScrapingService>();
builder.Services.AddScoped<HakemYorumuToplamaServisi>();
builder.Services.AddScoped<AIVideoAnalysisService>();
builder.Services.AddScoped<BeINSportsEmbedService>();
builder.Services.AddScoped<SkorCekmeServisi>();
builder.Services.AddScoped<PozisyonOtomatikTespitServisi>();
builder.Services.AddScoped<FiksturGuncellemeServisi>();

// Background Servisleri
builder.Services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<IBackgroundJobService>() as BackgroundJobService);
builder.Services.AddHostedService<MacTakipBackgroundService>();

// Session desteği (opsiyonel - anket için kullanılabilir)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS politikası (production için)
    app.UseHsts();
}
else
{
    // Development'ta detaylı hata sayfası
    app.UseDeveloperExceptionPage();
}

// Cloud Run ve proxy desteği için
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// HTTPS yönlendirmesi (production için)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Session middleware (UseRouting'den sonra, UseEndpoints'ten önce)
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database migration kontrolü
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Production'da otomatik migration uygula
        if (app.Environment.IsProduction())
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration başarıyla uygulandı");
        }
        else
        {
            // Development'ta pending migration kontrolü
            var pendingMigrations = dbContext.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                logger.LogWarning("Bekleyen {Count} migration var. 'Update-Database' komutunu çalıştırın.", pendingMigrations.Count());
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration sırasında hata oluştu");
        if (!app.Environment.IsDevelopment())
        {
            throw; // Production'da hata fırlat
        }
    }
}

app.Run();