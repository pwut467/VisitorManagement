using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.FrontDesk)]
public class CheckOutController : Controller
{
    private readonly AppDbContext _db;
    private readonly IVisitRegistrationService _registration;

    public CheckOutController(AppDbContext db, IVisitRegistrationService registration)
    {
        _db = db;
        _registration = registration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? code, string? returnUrl)
    {
        ViewBag.Gates = await LoadGatesAsync(null);
        ViewBag.Code = code;
        ViewBag.ReturnUrl = LocalReturnUrl.IsUsable(returnUrl, Url.IsLocalUrl) ? returnUrl : null;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string code, int? gateOutId, string? notes, string? returnUrl)
    {
        gateOutId ??= await DefaultGateIdAsync();
        ViewBag.Gates = await LoadGatesAsync(gateOutId);
        ViewBag.Code = code;
        ViewBag.ReturnUrl = LocalReturnUrl.IsUsable(returnUrl, Url.IsLocalUrl) ? returnUrl : null;

        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(string.Empty, "กรุณาสแกนบัตรหรือกรอกรหัส Visitor");
            return View();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var result = await _registration.CheckOutAsync(code, gateOutId, userId, notes);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return View();
        }

        TempData["Success"] = $"Check-out {result.Visit!.VisitNumber} เวลา {result.Visit.CheckOutAt:HH:mm} เรียบร้อย";
        return RedirectToAction("Details", "Visits", new { id = result.Visit.Id, returnUrl = ViewBag.ReturnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { found = false });
        }

        var key = code.Trim();
        if (key.StartsWith("VISIT|", StringComparison.OrdinalIgnoreCase))
        {
            key = key[6..];
        }

        var visit = await _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee)
            .Include(v => v.VisitorType)
            .FirstOrDefaultAsync(v => v.VisitCode == key || v.VisitNumber == key);

        if (visit is null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            visit.Id,
            visit.VisitNumber,
            visit.Status,
            statusText = visit.Status.ToString(),
            name = visit.GuestFullName,
            company = visit.CompanyName,
            host = visit.HostEmployee.FullName,
            photo = visit.PhotoPath,
            checkInAt = visit.CheckInAt?.ToString("dd/MM/yyyy HH:mm"),
            onSite = visit.Status == VisitStatus.CheckedIn
        });
    }

    private async Task<List<SelectListItem>> LoadGatesAsync(int? selectedId)
    {
        var gates = await _db.Gates.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
        var defaultId = selectedId
            ?? gates.FirstOrDefault(g => g.Name == "ประตูใหญ่")?.Id
            ?? gates.FirstOrDefault()?.Id;
        return gates
            .Select(g => new SelectListItem(g.Name, g.Id.ToString(), g.Id == defaultId))
            .ToList();
    }

    private Task<int?> DefaultGateIdAsync() =>
        _db.Gates.Where(g => g.IsActive && g.Name == "ประตูใหญ่")
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync();
}
