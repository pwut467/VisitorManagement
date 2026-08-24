using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SettingsController : Controller
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var c = await _db.CompanyProfiles.FirstAsync();
        return View(new SettingsViewModel
        {
            Name = c.Name,
            Address = c.Address,
            BadgeFooter = c.BadgeFooter,
            DefaultVisitHours = c.DefaultVisitHours,
            OverstayGraceMinutes = c.OverstayGraceMinutes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var c = await _db.CompanyProfiles.FirstAsync();
        c.Name = model.Name.Trim();
        c.Address = model.Address?.Trim();
        c.BadgeFooter = model.BadgeFooter.Trim();
        c.DefaultVisitHours = model.DefaultVisitHours;
        c.OverstayGraceMinutes = model.OverstayGraceMinutes;
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกการตั้งค่าแล้ว";
        return RedirectToAction(nameof(Index));
    }
}
