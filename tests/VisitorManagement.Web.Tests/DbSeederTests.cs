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

        var provider = services.BuildServiceProvider();
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
        Assert.Equal(1, await db.Visits.CountAsync());
    }
}
