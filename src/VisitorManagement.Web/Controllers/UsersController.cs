using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ApplicationUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _users.Users.OrderBy(u => u.UserName).ToListAsync();
        var rows = new List<(ApplicationUser User, IList<string> Roles, bool CanDelete)>();
        foreach (var u in users)
        {
            rows.Add((u, await _users.GetRolesAsync(u), CanDelete(u.UserName)));
        }

        return View(rows);
    }

    public IActionResult Create() => View("Edit", new UserFormViewModel());

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _users.GetRolesAsync(user);
        return View(ToForm(user, roles.FirstOrDefault() ?? AppRoles.Security));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.Id = null;
        model.IsOfficialAccount = false;
        model.RoleLocked = false;

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "กรุณากำหนดรหัสผ่าน");
        }
        else if (model.Password.Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "รหัสผ่านอย่างน้อย 6 ตัวอักษร");
        }

        NormalizeModel(model);
        if (!ValidateRole(model))
        {
            return View("Edit", model);
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        if (await UserNameTakenAsync(model.UserName, excludingUserId: null))
        {
            ModelState.AddModelError(nameof(model.UserName), "ชื่อผู้ใช้นี้ถูกใช้แล้ว");
            return View("Edit", model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            IsActive = model.IsActive
        };

        var result = await _users.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View("Edit", model);
        }

        await SetExclusiveRoleAsync(user, model.Role);
        TempData["Success"] = $"สร้างผู้ใช้ {user.UserName} แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            return BadRequest();
        }

        var user = await _users.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        var official = IsProtectedAdmin(user.UserName);
        model.IsOfficialAccount = official;
        model.RoleLocked = IsSecurityOnlyAccount(user.UserName);
        if (model.RoleLocked)
        {
            model.Role = AppRoles.Security;
        }

        if (official)
        {
            model.UserName = user.UserName ?? model.UserName;
        }

        NormalizeModel(model);
        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "รหัสผ่านอย่างน้อย 6 ตัวอักษร");
        }

        if (!ValidateRole(model))
        {
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!official && await UserNameTakenAsync(model.UserName, excludingUserId: user.Id))
        {
            ModelState.AddModelError(nameof(model.UserName), "ชื่อผู้ใช้นี้ถูกใช้แล้ว");
            return View(model);
        }

        if (!official)
        {
            var rename = await _users.SetUserNameAsync(user, model.UserName);
            if (!rename.Succeeded)
            {
                AddIdentityErrors(rename);
                return View(model);
            }
        }

        user.FullName = model.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        user.EmailConfirmed = true;
        user.IsActive = model.IsActive;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            AddIdentityErrors(update);
            return View(model);
        }

        await SetExclusiveRoleAsync(user, model.Role);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var reset = await _users.ResetPasswordAsync(user, token, model.Password);
            if (!reset.Succeeded)
            {
                AddIdentityErrors(reset);
                return View(model);
            }
        }

        TempData["Success"] = $"บันทึกผู้ใช้ {user.UserName} แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (IsProtectedAdmin(user.UserName))
        {
            TempData["Error"] = $"ไม่สามารถลบบัญชีระบบ {user.UserName} ได้";
            return RedirectToAction(nameof(Index));
        }

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(user.Id, currentId, StringComparison.Ordinal))
        {
            TempData["Error"] = "ไม่สามารถลบบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(Index));
        }

        if (await _users.IsInRoleAsync(user, AppRoles.Admin))
        {
            var adminCount = 0;
            foreach (var admin in await _users.GetUsersInRoleAsync(AppRoles.Admin))
            {
                if (admin.IsActive)
                {
                    adminCount++;
                }
            }

            if (adminCount <= 1)
            {
                TempData["Error"] = "ต้องเหลือผู้ดูแลระบบ (Admin) อย่างน้อย 1 คน";
                return RedirectToAction(nameof(Index));
            }
        }

        await ClearUserReferencesAsync(user.Id);
        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"ลบผู้ใช้ {user.UserName} แล้ว";
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

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(user.Id, currentId, StringComparison.Ordinal) && user.IsActive)
        {
            TempData["Error"] = "ไม่สามารถปิดบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _users.UpdateAsync(user);
        TempData["Success"] = user.IsActive ? $"เปิดใช้งาน {user.UserName} แล้ว" : $"ปิดใช้งาน {user.UserName} แล้ว";
        return RedirectToAction(nameof(Index));
    }

    private static UserFormViewModel ToForm(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email,
        Role = role,
        IsActive = user.IsActive,
        IsOfficialAccount = IsProtectedAdmin(user.UserName),
        RoleLocked = IsSecurityOnlyAccount(user.UserName)
    };

    private static void NormalizeModel(UserFormViewModel model)
    {
        model.UserName = model.UserName.Trim();
        model.FullName = model.FullName.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.Role = model.Role.Trim();
    }

    private bool ValidateRole(UserFormViewModel model)
    {
        if (model.RoleLocked)
        {
            model.Role = AppRoles.Security;
            return true;
        }

        if (!AppRoles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "สิทธิ์ไม่ถูกต้อง");
            return false;
        }

        return true;
    }

    private async Task<bool> UserNameTakenAsync(string userName, string? excludingUserId)
    {
        var existing = await _users.FindByNameAsync(userName);
        return existing is not null && !string.Equals(existing.Id, excludingUserId, StringComparison.Ordinal);
    }

    private async Task SetExclusiveRoleAsync(ApplicationUser user, string role)
    {
        await DbSeeder.EnsureExclusiveRoleAsync(_users, user, role);
    }

    private async Task ClearUserReferencesAsync(string userId)
    {
        foreach (var emp in await _db.Employees.Where(e => e.UserId == userId).ToListAsync())
        {
            emp.UserId = null;
        }

        foreach (var visit in await _db.Visits
                     .Where(v => v.RegisteredByUserId == userId || v.CheckedOutByUserId == userId)
                     .ToListAsync())
        {
            if (visit.RegisteredByUserId == userId)
            {
                visit.RegisteredByUserId = null;
            }

            if (visit.CheckedOutByUserId == userId)
            {
                visit.CheckedOutByUserId = null;
            }
        }

        await _db.SaveChangesAsync();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static bool IsProtectedAdmin(string? userName) =>
        string.Equals(userName, "SKAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool CanDelete(string? userName) => !IsProtectedAdmin(userName);

    private static bool IsSecurityOnlyAccount(string? userName) =>
        string.Equals(userName, "9641", StringComparison.OrdinalIgnoreCase);
}
