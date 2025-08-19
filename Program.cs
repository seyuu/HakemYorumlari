using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
