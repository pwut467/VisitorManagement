using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Tests;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsyncCreatesMasterDataAndDemoUsers()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        await DbSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Equal(5, await db.Employees.CountAsync());
        Assert.True(await db.Employees.AnyAsync(e => e.EmployeeCode == "5300162"));
        Assert.NotNull(await users.FindByEmailAsync("admin@company.local"));
        Assert.NotNull(await users.FindByEmailAsync("host@company.local"));

        var host = await db.Employees.FirstAsync(e => e.EmployeeCode == "5300162");
        Assert.False(string.IsNullOrEmpty(host.UserId));
        Assert.Equal(1, await db.Visits.CountAsync());
    }
}
