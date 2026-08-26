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
    private readonly ICloudVisitSyncService _cloudSync;
    private readonly ICompanyContext _companyContext;

    public VisitsController(AppDbContext db, IQrCodeService qr, ICloudVisitSyncService cloudSync, ICompanyContext companyContext)
    {
        _db = db;
        _qr = qr;
        _cloudSync = cloudSync;
        _companyContext = companyContext;
    }

    public async Task<IActionResult> Index(VisitListFilter filter)
    {
        var company = await _companyContext.GetActiveAsync();
        var q = _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.GateIn)
            .Where(v => v.CompanyProfileId == company.Id);

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
                v.GuestFirstName.Contains(term) ||
                v.GuestLastName.Contains(term) ||
                v.Visitor.FirstName.Contains(term) ||
                v.Visitor.LastName.Contains(term) ||
                v.Visitor.NationalId.Contains(term) ||
                (v.CompanyName != null && v.CompanyName.Contains(term)) ||
                (v.VehiclePlate != null && v.VehiclePlate.Contains(term)));
        }

        var list = await q.OrderByDescending(v => v.CreatedAt).Take(300).ToListAsync();
        ViewBag.Filter = filter;
        ViewBag.ActiveCompany = company;
        return View(list);
    }

    public async Task<IActionResult> OnSite()
    {
        var company = await _companyContext.GetActiveAsync();
        var list = await _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.GateIn)
            .Where(v => v.CompanyProfileId == company.Id && v.Status == VisitStatus.CheckedIn)
            .OrderBy(v => v.CheckInAt)
            .ToListAsync();
        ViewBag.Now = TimeHelper.Now;
        ViewBag.ActiveCompany = company;
        return View(list);
    }

    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var visit = await LoadAsync(id);
        if (visit is null)
        {
            return NotFound();
        }

        ViewBag.ReturnUrl = ResolveReturnUrl(returnUrl);
        ViewBag.Now = TimeHelper.Now;
        return View(visit);
    }

    [Authorize(Roles = AppRoles.FrontDesk)]
    public async Task<IActionResult> Badge(int id, string? autoprint = null, string? returnUrl = null)
    {
        var visit = await LoadAsync(id);
        if (visit is null)
        {
            return NotFound();
        }

        visit.BadgePrintedAt = TimeHelper.Now;
        await _db.SaveChangesAsync();

        ViewBag.BarcodeSvg = Code128Barcode.Svg(visit.VisitNumber);
        ViewBag.Company = visit.CompanyProfile
            ?? await _db.CompanyProfiles.FirstAsync(c => c.Id == visit.CompanyProfileId);
        ViewBag.AutoPrint = VisitorManagement.Web.ViewFlag.IsOn(autoprint);
        ViewBag.ReturnUrl = LocalReturnUrl.IsUsable(returnUrl, Url.IsLocalUrl) ? returnUrl : null;
        return View(visit);
    }

    [Authorize(Roles = AppRoles.FrontDesk)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, string? returnUrl)
    {
        var company = await _companyContext.GetActiveAsync();
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id && v.CompanyProfileId == company.Id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status == VisitStatus.CheckedOut)
        {
            TempData["Error"] = "รายการนี้ออกจากพื้นที่แล้ว ไม่สามารถยกเลิกได้";
            return RedirectToAction(nameof(Details), new { id, returnUrl });
        }

        visit.Status = VisitStatus.Cancelled;
        visit.Notes = string.IsNullOrWhiteSpace(visit.Notes) ? reason : visit.Notes + " | ยกเลิก: " + reason;
        visit.CloudSynced = false;
        visit.CloudSyncError = null;
        await _db.SaveChangesAsync();
        await _cloudSync.TrySyncVisitAsync(visit.Id);
        TempData["Success"] = "ยกเลิกรายการแล้ว";
        return RedirectToAction(nameof(Details), new { id, returnUrl });
    }

    public IActionResult Qr(string code)
    {
        var png = _qr.Png("VISIT|" + code, 8);
        return File(png, "image/png");
    }

    private async Task<Visit?> LoadAsync(int id)
    {
        var company = await _companyContext.GetActiveAsync();
        return await _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.CompanyProfile)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.VisitPurpose)
            .Include(v => v.GateIn)
            .Include(v => v.GateOut)
            .Include(v => v.RegisteredByUser)
            .Include(v => v.CheckedOutByUser)
            .FirstOrDefaultAsync(v => v.Id == id && v.CompanyProfileId == company.Id);
    }

    private string ResolveReturnUrl(string? returnUrl) =>
        LocalReturnUrl.Resolve(
            returnUrl,
            Request.Headers.Referer.ToString(),
            Url.IsLocalUrl,
            Url.Action(nameof(Index)) ?? "/Visits");
}
