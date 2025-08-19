using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Entity Framework - Production için özel yapılandırma
if (builder.Environment.IsProduction())
{
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=94.73.151.19;Database=u7401826_hakem;User Id=u7401826_hakem;Password=zdv@4-B8j.X3:I3R;TrustServerCertificate=true;MultipleActiveResultSets=true;";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// HttpClient ekle
builder.Services.AddHttpClient();

// Servisleri ekle (doğru sırayla - dependency injection için)
builder.Services.AddScoped<YouTubeScrapingService>();
builder.Services.AddScoped<TVKanalScrapingService>();
builder.Services.AddScoped<BeINSportsEmbedService>();
builder.Services.AddScoped<HakemYorumuToplamaServisi>();
builder.Services.AddScoped<SkorCekmeServisi>();
builder.Services.AddScoped<PozisyonOtomatikTespitServisi>();

// Background Service
builder.Services.AddHostedService<MacTakipBackgroundService>();

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
