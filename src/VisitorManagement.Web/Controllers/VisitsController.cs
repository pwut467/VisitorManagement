using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize]
public class VisitsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;

    public VisitsController(AppDbContext db, IQrCodeService qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<IActionResult> Index(VisitListFilter filter)
    {
        var q = _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.GateIn)
            .AsQueryable();

        if (filter.OnSiteOnly)
        {
            q = q.Where(v => v.Status == VisitStatus.CheckedIn);
        }

        if (filter.Status is VisitStatus st)
        {
            q = q.Where(v => v.Status == st);
        }

        if (filter.From is DateTime from)
        {
            q = q.Where(v => (v.CheckInAt ?? v.CreatedAt) >= from.Date);
        }

        if (filter.To is DateTime to)
        {
            var end = to.Date.AddDays(1);
            q = q.Where(v => (v.CheckInAt ?? v.CreatedAt) < end);
        }

        if (filter.HostEmployeeId is int hostId)
        {
            q = q.Where(v => v.HostEmployeeId == hostId);
        }

        if (filter.VisitorTypeId is int typeId)
        {
            q = q.Where(v => v.VisitorTypeId == typeId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var term = filter.Q.Trim();
            q = q.Where(v =>
                v.VisitNumber.Contains(term) ||
                v.Visitor.FirstName.Contains(term) ||
                v.Visitor.LastName.Contains(term) ||
                v.Visitor.NationalId.Contains(term) ||
                (v.CompanyName != null && v.CompanyName.Contains(term)) ||
                (v.VehiclePlate != null && v.VehiclePlate.Contains(term)));
        }

        var list = await q.OrderByDescending(v => v.CreatedAt).Take(300).ToListAsync();
        ViewBag.Filter = filter;
        return View(list);
    }

    public async Task<IActionResult> OnSite()
    {
        var list = await _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.GateIn)
            .Where(v => v.Status == VisitStatus.CheckedIn)
            .OrderBy(v => v.CheckInAt)
            .ToListAsync();
        ViewBag.Now = TimeHelper.Now;
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var visit = await LoadAsync(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status == VisitStatus.CheckedIn)
        {
            ViewBag.Qr = _qr.DataUrl("VISIT|" + visit.VisitCode);
        }
        ViewBag.Now = TimeHelper.Now;
        return View(visit);
    }

    [Authorize(Roles = AppRoles.FrontDesk)]
    public async Task<IActionResult> Badge(int id, string? autoprint = null)
    {
        var visit = await LoadAsync(id);
        if (visit is null)
        {
            return NotFound();
        }

        visit.BadgePrintedAt = TimeHelper.Now;
        await _db.SaveChangesAsync();

        ViewBag.BarcodeSvg = Code128Barcode.Svg(visit.VisitNumber);
        ViewBag.Company = await _db.CompanyProfiles.FirstAsync();
        ViewBag.AutoPrint = VisitorManagement.Web.ViewFlag.IsOn(autoprint);
        return View(visit);
    }

    [Authorize(Roles = AppRoles.FrontDesk)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var visit = await _db.Visits.FindAsync(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status == VisitStatus.CheckedOut)
        {
            TempData["Error"] = "รายการนี้ออกจากพื้นที่แล้ว ไม่สามารถยกเลิกได้";
            return RedirectToAction(nameof(Details), new { id });
        }

        visit.Status = VisitStatus.Cancelled;
        visit.Notes = string.IsNullOrWhiteSpace(visit.Notes) ? reason : visit.Notes + " | ยกเลิก: " + reason;
        await _db.SaveChangesAsync();
        TempData["Success"] = "ยกเลิกรายการแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult Qr(string code)
    {
        var png = _qr.Png("VISIT|" + code, 8);
        return File(png, "image/png");
    }

    private Task<Visit?> LoadAsync(int id) =>
        _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.VisitPurpose)
            .Include(v => v.GateIn)
            .Include(v => v.GateOut)
            .Include(v => v.RegisteredByUser)
            .Include(v => v.CheckedOutByUser)
            .FirstOrDefaultAsync(v => v.Id == id);
}
