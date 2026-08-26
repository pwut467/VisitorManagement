using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SettingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICloudVisitSyncService _cloudSync;
    private readonly ICloudConnectionStatus _cloudStatus;
    private readonly ICloudOptionsProvider _cloudOptions;
    private readonly ICompanyContext _companyContext;

    public SettingsController(
        AppDbContext db,
        ICloudVisitSyncService cloudSync,
        ICloudConnectionStatus cloudStatus,
        ICloudOptionsProvider cloudOptions,
        ICompanyContext companyContext)
    {
        _db = db;
        _cloudSync = cloudSync;
        _cloudStatus = cloudStatus;
        _cloudOptions = cloudOptions;
        _companyContext = companyContext;
    }

    public async Task<IActionResult> Index()
    {
        var c = await _companyContext.GetActiveAsync();
        ViewBag.ActiveCompany = c;
        return View(await ToModelAsync(c));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        var c = await ResolveCompanyAsync(model.CompanyId);
        if (!ModelState.IsValid)
        {
            await FillCompanyOptionsAsync(model, c.Id);
            var opts = await _cloudOptions.GetAsync();
            model.CloudPasswordSet = !string.IsNullOrEmpty(c.CloudPassword) || !string.IsNullOrEmpty(opts.Password);
            ApplyStatus(model);
            return View(model);
        }

        var code = CompanyContext.NormalizeCode(model.CompanyCode);
        if (code.Length == 0)
        {
            ModelState.AddModelError(nameof(model.CompanyCode), "กรุณากรอกรหัสบริษัท");
            await FillCompanyOptionsAsync(model, c.Id);
            ApplyStatus(model);
            return View(model);
        }

        if (await _db.CompanyProfiles.AnyAsync(x => x.CompanyCode == code && x.Id != c.Id))
        {
            ModelState.AddModelError(nameof(model.CompanyCode), $"รหัสบริษัท '{code}' มีอยู่แล้ว");
            await FillCompanyOptionsAsync(model, c.Id);
            ApplyStatus(model);
            return View(model);
        }

        c.CompanyCode = code;
        c.Name = model.Name.Trim();
        c.Address = model.Address?.Trim();
        c.BadgeFooter = model.BadgeFooter.Trim();
        c.DefaultVisitHours = model.DefaultVisitHours;
        c.OverstayGraceMinutes = model.OverstayGraceMinutes;
        c.AutoPrintBadge = model.AutoPrintBadge;
        c.CloudEnabled = model.CloudEnabled;
        c.CloudServer = string.IsNullOrWhiteSpace(model.CloudServer) ? "192.168.11.204" : model.CloudServer.Trim();
        c.CloudDatabase = string.IsNullOrWhiteSpace(model.CloudDatabase) ? "VisitorManagment" : model.CloudDatabase.Trim();
        c.CloudUseWindowsAuth = model.CloudUseWindowsAuth;
        c.CloudUserId = model.CloudUserId?.Trim();
        if (!string.IsNullOrWhiteSpace(model.CloudPassword))
        {
            c.CloudPassword = model.CloudPassword;
        }
        else if (model.CloudUseWindowsAuth)
        {
            c.CloudPassword = null;
        }

        await _db.SaveChangesAsync();
        await _companyContext.SetActiveAsync(c.Id);
        await _cloudSync.ProbeAsync();
        var synced = await _cloudSync.SyncPendingAsync();
        TempData["Success"] = synced > 0
            ? $"บันทึกการตั้งค่าบริษัท {c.CompanyCode} แล้ว และซิงก์ไป Cloud {synced} รายการ"
            : $"บันทึกการตั้งค่าบริษัท {c.CompanyCode} แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchCompany(int companyId)
    {
        await _companyContext.SetActiveAsync(companyId);
        var c = await _companyContext.GetActiveAsync();
        TempData["Success"] = $"สลับไปบริษัท {c.CompanyCode} — {c.Name}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCompany(string? newCompanyCode, string? newCompanyName)
    {
        try
        {
            var created = await _companyContext.CreateAsync(newCompanyCode ?? "", newCompanyName ?? "");
            TempData["Success"] = $"สร้างบริษัท {created.CompanyCode} แล้ว — ข้อมูลผู้มาติดต่อจะแยกตามรหัสนี้";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestCloud(SettingsViewModel model)
    {
        var c = await ResolveCompanyAsync(model.CompanyId);
        c.CloudEnabled = model.CloudEnabled;
        c.CloudServer = string.IsNullOrWhiteSpace(model.CloudServer) ? "192.168.11.204" : model.CloudServer.Trim();
        c.CloudDatabase = string.IsNullOrWhiteSpace(model.CloudDatabase) ? "VisitorManagment" : model.CloudDatabase.Trim();
        c.CloudUseWindowsAuth = model.CloudUseWindowsAuth;
        c.CloudUserId = model.CloudUserId?.Trim();
        if (!string.IsNullOrWhiteSpace(model.CloudPassword))
        {
            c.CloudPassword = model.CloudPassword;
        }
        else if (model.CloudUseWindowsAuth)
        {
            c.CloudPassword = null;
        }

        await _db.SaveChangesAsync();
        await _companyContext.SetActiveAsync(c.Id);

        var ok = await _cloudSync.ProbeAsync();
        var snap = _cloudStatus.Current;
        if (!ok)
        {
            TempData["Error"] = $"เชื่อมต่อ Cloud ไม่สำเร็จ — {snap.LastError}";
            return RedirectToAction(nameof(Index));
        }

        var synced = await _cloudSync.SyncPendingAsync();
        TempData["Success"] = synced > 0
            ? $"เชื่อมต่อ Cloud สำเร็จ — {snap.Server}/{snap.Database} · ซิงก์แล้ว {synced} รายการ"
            : $"เชื่อมต่อ Cloud สำเร็จ — {snap.Server}/{snap.Database}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncPending()
    {
        var ok = await _cloudSync.ProbeAsync();
        if (!ok)
        {
            var snap = _cloudStatus.Current;
            TempData["Error"] = $"ซิงก์ไม่สำเร็จ — {snap.LastError}";
            return RedirectToAction(nameof(Index));
        }

        var synced = await _cloudSync.SyncPendingAsync();
        var pending = _cloudStatus.Current.PendingSyncCount;
        TempData["Success"] = synced > 0
            ? $"ซิงก์ไป Cloud แล้ว {synced} รายการ" + (pending > 0 ? $" (ยังค้าง {pending})" : "")
            : pending > 0
                ? $"ไม่มีการซิงก์สำเร็จ — ยังค้าง {pending} รายการ (ดู CloudSyncError ในรายละเอียดบัตร)"
                : "ไม่มีรายการค้างซิงก์";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearVisitors()
    {
        var company = await _companyContext.GetActiveAsync();
        var visitCount = await _db.Visits.CountAsync(v => v.CompanyProfileId == company.Id);
        var visitorCount = await _db.Visitors.CountAsync(v => v.CompanyProfileId == company.Id);
        await DbSeeder.ClearVisitorDataForCompanyAsync(_db, company.Id);
        _cloudStatus.SetPendingCount(await _db.Visits.CountAsync(v => !v.CloudSynced));
        TempData["Success"] = $"ล้างข้อมูลผู้มาติดต่อของบริษัท {company.CompanyCode} แล้ว (บัตร {visitCount} / บุคคล {visitorCount})";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CompanyProfile> ResolveCompanyAsync(int companyId)
    {
        if (companyId > 0)
        {
            var found = await _db.CompanyProfiles.FirstOrDefaultAsync(c => c.Id == companyId);
            if (found is not null)
            {
                return found;
            }
        }

        return await _companyContext.GetActiveAsync();
    }

    private async Task<SettingsViewModel> ToModelAsync(CompanyProfile c)
    {
        var opts = await _cloudOptions.GetAsync();
        var model = new SettingsViewModel
        {
            CompanyId = c.Id,
            CompanyCode = c.CompanyCode,
            Name = c.Name,
            Address = c.Address,
            BadgeFooter = c.BadgeFooter,
            DefaultVisitHours = c.DefaultVisitHours,
            OverstayGraceMinutes = c.OverstayGraceMinutes,
            AutoPrintBadge = c.AutoPrintBadge,
            CloudEnabled = c.CloudEnabled,
            CloudServer = string.IsNullOrWhiteSpace(c.CloudServer) ? opts.Server : c.CloudServer,
            CloudDatabase = string.IsNullOrWhiteSpace(c.CloudDatabase) ? opts.Database : c.CloudDatabase,
            CloudUseWindowsAuth = c.CloudUseWindowsAuth,
            CloudUserId = string.IsNullOrWhiteSpace(c.CloudUserId) ? opts.UserId : c.CloudUserId,
            CloudPasswordSet = !string.IsNullOrEmpty(c.CloudPassword) || !string.IsNullOrEmpty(opts.Password)
        };
        await FillCompanyOptionsAsync(model, c.Id);
        ApplyStatus(model);
        return model;
    }

    private async Task FillCompanyOptionsAsync(SettingsViewModel model, int selectedId)
    {
        var list = await _companyContext.ListAsync();
        model.CompanyOptions = list
            .Select(c => new SelectListItem($"{c.CompanyCode} — {c.Name}", c.Id.ToString(), c.Id == selectedId))
            .ToList();
    }

    private void ApplyStatus(SettingsViewModel model)
    {
        var snap = _cloudStatus.Current;
        model.CloudOnline = snap.Online;
        model.CloudStatusLabel = !snap.Enabled
            ? "Cloud ปิดใช้งาน"
            : !snap.Configured
                ? "Cloud ยังไม่ได้ตั้งค่า"
                : snap.Online
                    ? "Cloud ออนไลน์"
                    : "Cloud ออฟไลน์";
        model.CloudStatusDetail = snap.Online
            ? $"{snap.Server} / {snap.Database}"
            : snap.LastError
              ?? (!snap.Configured
                  ? "กรอก Username/Password (SQL Auth) ของเซิร์ฟเวอร์ 192.168.11.204 แล้วกดทดสอบ"
                  : null);
    }
}
