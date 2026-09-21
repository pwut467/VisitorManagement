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
            var normalized = code.Trim().ToUpperInvariant();
            if (await _db.Departments.AnyAsync(d => d.Code == normalized))
            {
                TempData["Error"] = $"รหัสแผนก '{normalized}' มีอยู่แล้ว";
                return RedirectToAction(nameof(Index));
            }

            _db.Departments.Add(new Department { Code = normalized, Name = name.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "เพิ่มแผนกแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDepartment(int id, string code, string name)
    {
        var d = await _db.Departments.FindAsync(id);
        if (d is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "กรุณากรอกรหัสและชื่อแผนก";
            return RedirectToAction(nameof(Index));
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (await _db.Departments.AnyAsync(x => x.Code == normalized && x.Id != id))
        {
            TempData["Error"] = $"รหัสแผนก '{normalized}' มีอยู่แล้ว";
            return RedirectToAction(nameof(Index));
        }

        d.Code = normalized;
        d.Name = name.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกแผนกแล้ว";
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
    public async Task<IActionResult> EditGate(int id, string name, string? location)
    {
        var g = await _db.Gates.FindAsync(id);
        if (g is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "กรุณากรอกชื่อจุดเข้า-ออก";
            return RedirectToAction(nameof(Index));
        }

        g.Name = name.Trim();
        g.Location = location?.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกจุดเข้า-ออกแล้ว";
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
    public async Task<IActionResult> EditType(int id, string name, string? badgeLabel, string? color)
    {
        var t = await _db.VisitorTypes.FindAsync(id);
        if (t is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "กรุณากรอกชื่อประเภท";
            return RedirectToAction(nameof(Index));
        }

        t.Name = name.Trim();
        t.BadgeLabel = string.IsNullOrWhiteSpace(badgeLabel) ? "VISITOR" : badgeLabel.Trim().ToUpperInvariant();
        t.Color = string.IsNullOrWhiteSpace(color) ? t.Color : color;
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกประเภทผู้มาติดต่อแล้ว";
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
    public async Task<IActionResult> EditPurpose(int id, string name)
    {
        var p = await _db.VisitPurposes.FindAsync(id);
        if (p is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "กรุณากรอกวัตถุประสงค์";
            return RedirectToAction(nameof(Index));
        }

        p.Name = name.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกวัตถุประสงค์แล้ว";
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string entity, int id)
    {
        switch (entity)
        {
            case "department":
            {
                var d = await _db.Departments.FindAsync(id);
                if (d is null) return NotFound();
                if (await _db.Employees.AnyAsync(e => e.DepartmentId == id))
                {
                    TempData["Error"] = $"ลบแผนก '{d.Name}' ไม่ได้ เพราะมีพนักงานใช้อยู่ — ย้ายพนักงานก่อน หรือปิดการใช้งานแทน";
                    return RedirectToAction(nameof(Index));
                }

                _db.Departments.Remove(d);
                TempData["Success"] = $"ลบแผนก {d.Name} แล้ว";
                break;
            }
            case "gate":
            {
                var g = await _db.Gates.FindAsync(id);
                if (g is null) return NotFound();
                if (await _db.Visits.AnyAsync(v => v.GateInId == id || v.GateOutId == id))
                {
                    TempData["Error"] = $"ลบจุดเข้า-ออก '{g.Name}' ไม่ได้ เพราะถูกใช้ในประวัติการเข้าพบ — ปิดการใช้งานแทน";
                    return RedirectToAction(nameof(Index));
                }

                _db.Gates.Remove(g);
                TempData["Success"] = $"ลบจุดเข้า-ออก {g.Name} แล้ว";
                break;
            }
            case "type":
            {
                var t = await _db.VisitorTypes.FindAsync(id);
                if (t is null) return NotFound();
                if (await _db.Visits.AnyAsync(v => v.VisitorTypeId == id))
                {
                    TempData["Error"] = $"ลบประเภท '{t.Name}' ไม่ได้ เพราะถูกใช้ในประวัติการเข้าพบ — ปิดการใช้งานแทน";
                    return RedirectToAction(nameof(Index));
                }

                _db.VisitorTypes.Remove(t);
                TempData["Success"] = $"ลบประเภท {t.Name} แล้ว";
                break;
            }
            case "purpose":
            {
                var p = await _db.VisitPurposes.FindAsync(id);
                if (p is null) return NotFound();
                if (await _db.Visits.AnyAsync(v => v.VisitPurposeId == id))
                {
                    TempData["Error"] = $"ลบวัตถุประสงค์ '{p.Name}' ไม่ได้ เพราะถูกใช้ในประวัติการเข้าพบ — ปิดการใช้งานแทน";
                    return RedirectToAction(nameof(Index));
                }

                _db.VisitPurposes.Remove(p);
                TempData["Success"] = $"ลบวัตถุประสงค์ {p.Name} แล้ว";
                break;
            }
            default:
                TempData["Error"] = "ไม่รู้จักประเภทข้อมูล";
                return RedirectToAction(nameof(Index));
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
