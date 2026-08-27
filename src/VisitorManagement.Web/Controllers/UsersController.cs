using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;

    public UsersController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _users.Users.OrderBy(u => u.UserName).ToListAsync();
        var rows = new List<(ApplicationUser User, IList<string> Roles)>();
        foreach (var u in users)
        {
            rows.Add((u, await _users.GetRolesAsync(u)));
        }

        return View(rows);
    }

    public IActionResult Create() => View(new UserFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "กรุณากำหนดรหัสผ่าน");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            IsActive = model.IsActive
        };
        var result = await _users.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
            {
                ModelState.AddModelError(string.Empty, e.Description);
            }

            return View(model);
        }

        if (AppRoles.All.Contains(model.Role))
        {
            await _users.AddToRoleAsync(user, model.Role);
        }

        TempData["Success"] = "สร้างผู้ใช้แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        await _users.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }
}
