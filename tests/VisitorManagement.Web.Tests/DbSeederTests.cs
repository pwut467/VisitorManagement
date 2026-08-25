using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Tests;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsyncReplacesUsersWithOfficialAccounts()
    {
        var provider = CreateIdentityServices();
        await DbSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Equal(5, await db.Employees.CountAsync());
        Assert.Equal(2, await users.Users.CountAsync());

        var admin = await users.FindByNameAsync("SKAdmin");
        var security = await users.FindByNameAsync("9641");
        Assert.NotNull(admin);
        Assert.NotNull(security);
        Assert.True(await users.CheckPasswordAsync(admin, "123456"));
        Assert.True(await users.CheckPasswordAsync(security, "123456"));
        Assert.True(await users.IsInRoleAsync(admin, AppRoles.Admin));
        Assert.True(await users.IsInRoleAsync(security, AppRoles.Security));
        Assert.Null(await users.FindByNameAsync("admin@company.local"));
        Assert.True(await db.Employees.AllAsync(e => e.UserId == null));
        Assert.Equal(0, await db.Visits.CountAsync());
        Assert.Equal(0, await db.Visitors.CountAsync());
    }

    [Fact]
    public async Task SeedAsyncKeepsOfficialUserIdsOnSubsequentRuns()
    {
        var provider = CreateIdentityServices();
        await DbSeeder.SeedAsync(provider);

        string adminId;
        string securityId;
        string? adminStamp;
        using (var scope = provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByNameAsync("SKAdmin");
            var security = await users.FindByNameAsync("9641");
            Assert.NotNull(admin);
            Assert.NotNull(security);
            adminId = admin.Id;
            securityId = security.Id;
            adminStamp = admin.SecurityStamp;
        }

        await DbSeeder.SeedAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByNameAsync("SKAdmin");
            var security = await users.FindByNameAsync("9641");
            Assert.NotNull(admin);
            Assert.NotNull(security);
            Assert.Equal(adminId, admin.Id);
            Assert.Equal(securityId, security.Id);
            Assert.Equal(adminStamp, admin.SecurityStamp);
            Assert.Equal(2, await users.Users.CountAsync());
            Assert.True(await users.CheckPasswordAsync(admin, "123456"));
        }
    }

    [Fact]
    public async Task SeedAsyncRemovesLeftoverDemoUsersOnly()
    {
        var provider = CreateIdentityServices();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await db.Database.EnsureCreatedAsync();
            var leftover = new ApplicationUser
            {
                UserName = "admin@company.local",
                Email = "admin@company.local",
                EmailConfirmed = true,
                FullName = "Old Admin",
                IsActive = true
            };
            var created = await users.CreateAsync(leftover, "123456");
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await DbSeeder.SeedAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Null(await users.FindByNameAsync("admin@company.local"));
            Assert.NotNull(await users.FindByNameAsync("SKAdmin"));
            Assert.NotNull(await users.FindByNameAsync("9641"));
            Assert.Equal(2, await users.Users.CountAsync());
        }
    }

    private static ServiceProvider CreateIdentityServices()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        return services.BuildServiceProvider();
    }
}
