using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.FrontDesk)]
public class BlacklistController : Controller
{
    private readonly AppDbContext _db;

    public BlacklistController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.BlacklistEntries.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.CreatedAt).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new BlacklistFormViewModel());

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlacklistFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _db.BlacklistEntries.Add(new BlacklistEntry
        {
            NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : ThaiNationalId.Normalize(model.NationalId),
            FullName = model.FullName.Trim(),
            Reason = model.Reason.Trim(),
            ExpiresAt = model.ExpiresAt,
            IsActive = model.IsActive,
            CreatedAt = TimeHelper.Now,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "เพิ่มรายชื่อบัญชีดำแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await _db.BlacklistEntries.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
