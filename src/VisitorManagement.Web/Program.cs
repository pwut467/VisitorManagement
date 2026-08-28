using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Middleware;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Optional per-machine overrides (not committed). Copy from appsettings.Local.json.example.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var provider = builder.Environment.IsEnvironment("Testing")
    ? "InMemory"
    : builder.Configuration["Database:Provider"] ?? "SqlServer";

builder.Services.AddSingleton<AppStartupState>();
builder.Services.AddSingleton<SqlConnectionResolver>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    switch (provider)
    {
        case "InMemory":
            options.UseInMemoryDatabase("VisitorManagement");
            break;
        default:
            options.UseSqlServer(sp.GetRequiredService<SqlConnectionResolver>().ConnectionString);
            break;
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = false;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.Lockout.MaxFailedAccessAttempts = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("CardReaderAgent", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ICompanyContext, CompanyContext>();
builder.Services.AddScoped<IVisitNumberService, VisitNumberService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IPhotoStorageService, PhotoStorageService>();
builder.Services.AddScoped<IBlacklistService, BlacklistService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IVisitRegistrationService, VisitRegistrationService>();
builder.Services.AddSingleton<ICloudConnectionStatus, CloudConnectionStatus>();
builder.Services.AddSingleton<ICloudOptionsProvider, CloudOptionsProvider>();
builder.Services.AddScoped<ICloudVisitSyncService, CloudVisitSyncService>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<CloudSyncBackgroundService>();
}

var app = builder.Build();

var startupState = app.Services.GetRequiredService<AppStartupState>();
if (app.Environment.IsEnvironment("Testing"))
{
    startupState.MarkReady();
}
else
{
    try
    {
        await DbSeeder.SeedAsync(app.Services);
        startupState.MarkReady();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var message = ex.GetBaseException().Message;
        if (ex is InvalidOperationException ioe && !string.IsNullOrWhiteSpace(ioe.Message))
        {
            message = ioe.Message;
        }

        var logPath = WriteStartupErrorLog(app.Environment.ContentRootPath, message, ex);
        logger.LogCritical(ex, "Database bootstrap failed. See {LogPath}", logPath);
        startupState.MarkFailed(message, logPath);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseMiddleware<DatabaseReadyMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

static string WriteStartupErrorLog(string contentRoot, string message, Exception ex)
{
    try
    {
        var logsDir = Path.Combine(contentRoot, "logs");
        Directory.CreateDirectory(logsDir);
        var path = Path.Combine(logsDir, "startup-error.txt");
        var text =
            DateTimeOffset.Now.ToString("u") + Environment.NewLine +
            message + Environment.NewLine + Environment.NewLine +
            ex + Environment.NewLine;
        File.WriteAllText(path, text);
        return path;
    }
    catch
    {
        return "(เขียนไฟล์ logs/startup-error.txt ไม่สำเร็จ — ดู Event Viewer / stdout log)";
    }
}

public partial class Program;
