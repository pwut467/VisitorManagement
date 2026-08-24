using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class MasterDataController : Controller
{
    private readonly AppDbContext _db;

    public MasterDataController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Departments = await _db.Departments.OrderBy(x => x.Code).ToListAsync();
        ViewBag.Gates = await _db.Gates.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Types = await _db.VisitorTypes.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Purposes = await _db.VisitPurposes.OrderBy(x => x.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDepartment(string code, string name)
    {
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            _db.Departments.Add(new Department { Code = code.Trim(), Name = name.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "เพิ่มแผนกแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGate(string name, string? location)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.Gates.Add(new Gate { Name = name.Trim(), Location = location?.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "เพิ่มจุดเข้า-ออกแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddType(string name, string? badgeLabel, string? color)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.VisitorTypes.Add(new VisitorType
            {
                Name = name.Trim(),
                BadgeLabel = string.IsNullOrWhiteSpace(badgeLabel) ? "VISITOR" : badgeLabel.Trim().ToUpperInvariant(),
                Color = string.IsNullOrWhiteSpace(color) ? "#1a56a0" : color
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "เพิ่มประเภทผู้มาติดต่อแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPurpose(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.VisitPurposes.Add(new VisitPurpose { Name = name.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "เพิ่มวัตถุประสงค์แล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string entity, int id)
    {
        switch (entity)
        {
            case "department":
                var d = await _db.Departments.FindAsync(id);
                if (d is not null) d.IsActive = !d.IsActive;
                break;
            case "gate":
                var g = await _db.Gates.FindAsync(id);
                if (g is not null) g.IsActive = !g.IsActive;
                break;
            case "type":
                var t = await _db.VisitorTypes.FindAsync(id);
                if (t is not null) t.IsActive = !t.IsActive;
                break;
            case "purpose":
                var p = await _db.VisitPurposes.FindAsync(id);
                if (p is not null) p.IsActive = !p.IsActive;
                break;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
