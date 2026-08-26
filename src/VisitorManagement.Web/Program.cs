using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Environment.IsEnvironment("Testing")
    ? "InMemory"
    : builder.Configuration["Database:Provider"] ?? "SqlServer";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider)
    {
        case "InMemory":
            options.UseInMemoryDatabase("VisitorManagement");
            break;
        default:
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")
                ?? @"Server=.\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True");
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

if (!app.Environment.IsEnvironment("Testing"))
{
    await DbSeeder.SeedAsync(app.Services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program;
