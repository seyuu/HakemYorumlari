using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Google.Cloud.AspNetCore.DataProtection.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Http.Resilience;
using Google.Cloud.Storage.V1; 

var builder = WebApplication.CreateBuilder(args);

// Console logging'i açık bir şekilde ekle
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Cloud Run için port yapılandırması
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Host validation'ı tamamen devre dışı bırak
builder.WebHost.UseKestrel(options =>
{
    options.AllowSynchronousIO = true;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

// AllowedHosts'u temizle
builder.Configuration["AllowedHosts"] = "*";

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data Protection'ı Google Cloud Storage kullanacak şekilde yapılandır
try
{
    var projectId = "hakemyorumlama";
    var bucketName = "hakemyorumlari-dataprotection-keys"; // Oluşturduğunuz Cloud Storage bucket'ının adı

    var storageClient = StorageClient.Create();
    storageClient.GetBucket(bucketName); // Bucket'ın varlığını kontrol et

    builder.Services.AddDataProtection()
        .PersistKeysToGoogleCloudStorage(bucketName, "keys.xml", storageClient)
        .SetApplicationName("Hakemyorumlari");

    builder.Logging.Services.BuildServiceProvider().GetService<ILogger<Program>>()?.
        LogInformation("Data Protection, Google Cloud Storage'a başarıyla bağlandı. Bucket: {BucketName}", bucketName);
}
catch (Exception ex)
{
    Console.WriteLine($"[KRİTİK HATA] Data Protection yapılandırılamadı. Hata: {ex.Message}");
    throw new InvalidOperationException("Data Protection yapılandırması başarısız oldu.", ex);
}

// HttpClient servisleri - HEPSİNİ TEMİZLEDİK
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

builder.Services.AddHttpClient("DefaultHttpClient")
    .AddStandardResilienceHandler();

 //
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
     // Development modunda appsettings.json'dan oku
     var connectionString = builder.Configuration.GetConnectionString("SQL_CONNECTION_STRING");

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
 builder.Services.AddScoped<HakemYorumuToplamaServisi>();
 builder.Services.AddScoped<AIVideoAnalysisService>();
 builder.Services.AddScoped<BeINSportsEmbedService>();
 builder.Services.AddScoped<SkorCekmeServisi>();
 builder.Services.AddScoped<PozisyonOtomatikTespitServisi>();
 builder.Services.AddScoped<FiksturGuncellemeServisi>();
// builder.Services.AddHostedService<MacTakipBackgroundService>();

 // SADECE BU SATIRLARI BIRAK:
// builder.Services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
 //builder.Services.AddHostedService<BackgroundJobService>();

// Background Servisleri
builder.Services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<IBackgroundJobService>() as BackgroundJobService);
builder.Services.AddHostedService<MacTakipBackgroundService>();


 // WebApplication'ı oluştur
 var app = builder.Build();

 // Configure the HTTP request pipeline.
 if (!app.Environment.IsDevelopment())
 {
     app.UseExceptionHandler("/Home/Error");
     // Cloud Run için HTTPS redirection'ı kaldır
     // app.UseHsts(); // Bu da kaldırılabilir
 }

// EKLENDİ: Cloud Run gibi bir proxy arkasında çalışmak için gerekli
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

 // Cloud Run HTTP üzerinden çalıştığı için HTTPS redirection'ı kaldır
 // app.UseHttpsRedirection(); // Bu satırı kaldır veya yorum yap

 app.UseStaticFiles();

 app.UseRouting();

 app.UseAuthorization();

 app.MapControllerRoute(
     name: "default",
     pattern: "{controller=Home}/{action=Index}/{id?}");

 app.Run();