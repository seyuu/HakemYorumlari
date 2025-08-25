using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ***** Buradaki kısım ÖNEMLİ! HakemYorumuToplamaServisi için Typed HttpClient kaydı *****
// Bu, hem HakemYorumuToplamaServisi'ni DI'ya kaydeder hem de
// constructor'ında HttpClient beklediğinde otomatik olarak sağlar.
// OAuth2 kullanarak YouTube API'ye erişim sağlar.
builder.Services.AddHttpClient<HakemYorumlari.Services.HakemYorumuToplamaServisi>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
    // OAuth2 için Authorization header'ı runtime'da eklenir
    // Burada sadece temel yapılandırma yapıyoruz
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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("DefaultConnection connection string development ortamında zorunludur.");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Servisleri ekle
builder.Services.AddScoped<YouTubeScrapingService>();
builder.Services.AddScoped<TVKanalScrapingService>();
builder.Services.AddScoped<BeINSportsEmbedService>();
builder.Services.AddScoped<SkorCekmeServisi>();
builder.Services.AddScoped<PozisyonOtomatikTespitServisi>();
builder.Services.AddHostedService<MacTakipBackgroundService>();

var app = builder.Build();

// Cloud Run için port yapılandırması - EN BAŞTA!
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Logger.LogInformation($"Uygulama {port} portunda başlatılıyor...");

// Cloud Run için doğru port binding - EN BAŞTA!
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation($"Port {port} dinleniyor...");

// Database migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.Migrate();
        app.Logger.LogInformation("Veritabanı migration'ları başarıyla uygulandı");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Veritabanı migration hatası");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cloud Run'da HTTPS redirection kullanma
// app.UseHttpsRedirection(); // Bu satırı kaldırdık

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
