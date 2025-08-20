using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

var app = builder.Build();

// Cloud Run için port yapılandırması - EN BAŞTA!
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Logger.LogInformation($"Uygulama {port} portunda başlatılıyor...");

// Cloud Run için doğru port binding - EN BAŞTA!
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation($"Port {port} dinleniyor...");

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
