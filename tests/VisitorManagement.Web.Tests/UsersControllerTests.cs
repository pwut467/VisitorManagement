using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagement.Web.Controllers;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task CreateEditDelete_WorksForNonOfficialUser()
    {
        var provider = CreateServices();
        await DbSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = await users.FindByNameAsync("SKAdmin");
        Assert.NotNull(admin);

        var controller = CreateController(users, db, admin.Id);

        var create = await controller.Create(new UserFormViewModel
        {
            FullName = "พนักงานทดสอบ",
            UserName = "guard01",
            Password = "123456",
            ConfirmPassword = "123456",
            Role = AppRoles.Security,
            IsActive = true
        });
        Assert.IsType<RedirectToActionResult>(create);

        var created = await users.FindByNameAsync("guard01");
        Assert.NotNull(created);
        Assert.Equal(new[] { AppRoles.Security }, (await users.GetRolesAsync(created)).OrderBy(r => r));

        var editGet = await controller.Edit(created.Id);
        var editView = Assert.IsType<ViewResult>(editGet);
        var form = Assert.IsType<UserFormViewModel>(editView.Model);
        form.FullName = "พนักงานแก้ไข";
        form.Role = AppRoles.Reception;
        form.Password = null;
        form.ConfirmPassword = null;

        var editPost = await controller.Edit(form);
        Assert.IsType<RedirectToActionResult>(editPost);
        created = await users.FindByNameAsync("guard01");
        Assert.NotNull(created);
        Assert.Equal("พนักงานแก้ไข", created.FullName);
        Assert.Equal(new[] { AppRoles.Reception }, (await users.GetRolesAsync(created)).OrderBy(r => r));

        var delete = await controller.Delete(created.Id);
        Assert.IsType<RedirectToActionResult>(delete);
        Assert.Null(await users.FindByNameAsync("guard01"));
    }

    [Fact]
    public async Task Delete_AllowsSecurityUser_ButRejectsSkAdmin()
    {
        var provider = CreateServices();
        await DbSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = await users.FindByNameAsync("SKAdmin");
        var security = await users.FindByNameAsync("9641");
        Assert.NotNull(admin);
        Assert.NotNull(security);

        var controller = CreateController(users, db, admin.Id);

        var deleteSecurity = await controller.Delete(security.Id);
        Assert.IsType<RedirectToActionResult>(deleteSecurity);
        Assert.Null(await users.FindByNameAsync("9641"));

        var deleteAdmin = await controller.Delete(admin.Id);
        Assert.IsType<RedirectToActionResult>(deleteAdmin);
        Assert.NotNull(await users.FindByNameAsync("SKAdmin"));
        Assert.Contains("ไม่สามารถลบ", controller.TempData["Error"]?.ToString());
    }

    [Fact]
    public async Task Edit_LocksSecurityOnlyAccountToSecurityRole()
    {
        var provider = CreateServices();
        await DbSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = await users.FindByNameAsync("SKAdmin");
        var security = await users.FindByNameAsync("9641");
        Assert.NotNull(admin);
        Assert.NotNull(security);

        var controller = CreateController(users, db, admin.Id);
        var result = await controller.Edit(new UserFormViewModel
        {
            Id = security.Id,
            FullName = "รปภ.",
            UserName = "9641",
            Role = AppRoles.Admin,
            IsActive = true
        });
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(new[] { AppRoles.Security }, (await users.GetRolesAsync(security)).OrderBy(r => r));
        Assert.False(await users.IsInRoleAsync(security, AppRoles.Admin));
    }

    private static UsersController CreateController(UserManager<ApplicationUser> users, AppDbContext db, string adminId)
    {
        var controller = new UsersController(users, db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider()),
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, adminId),
                        new Claim(ClaimTypes.Name, "SKAdmin"),
                        new Claim(ClaimTypes.Role, AppRoles.Admin)
                    ], "Test"))
                }
            }
        };
        return controller;
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
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

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _data = new();

        public IDictionary<string, object?> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) =>
            _data = new Dictionary<string, object?>(values);
    }
}
