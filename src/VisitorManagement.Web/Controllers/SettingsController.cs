using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public SettingsController(
        AppDbContext db,
        ICloudVisitSyncService cloudSync,
        ICloudConnectionStatus cloudStatus,
        ICloudOptionsProvider cloudOptions)
    {
        _db = db;
        _cloudSync = cloudSync;
        _cloudStatus = cloudStatus;
        _cloudOptions = cloudOptions;
    }

    public async Task<IActionResult> Index()
    {
        var c = await _db.CompanyProfiles.FirstAsync();
        return View(await ToModelAsync(c));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var current = await _db.CompanyProfiles.FirstAsync();
            var opts = await _cloudOptions.GetAsync();
            model.CloudPasswordSet = !string.IsNullOrEmpty(current.CloudPassword) || !string.IsNullOrEmpty(opts.Password);
            if (string.IsNullOrWhiteSpace(model.CloudUserId))
            {
                model.CloudUserId = opts.UserId;
            }

            ApplyStatus(model);
            return View(model);
        }

        var c = await _db.CompanyProfiles.FirstAsync();
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
        await _cloudSync.ProbeAsync();
        var synced = await _cloudSync.SyncPendingAsync();
        TempData["Success"] = synced > 0
            ? $"บันทึกการตั้งค่าแล้ว และซิงก์ไป Cloud {synced} รายการ"
            : "บันทึกการตั้งค่าแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestCloud(SettingsViewModel model)
    {
        var c = await _db.CompanyProfiles.FirstAsync();
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
        var visitCount = await _db.Visits.CountAsync();
        var visitorCount = await _db.Visitors.CountAsync();
        await DbSeeder.ClearAllVisitorDataAsync(_db);
        _cloudStatus.SetPendingCount(0);
        TempData["Success"] = $"ล้างข้อมูลผู้มาติดต่อแล้ว (บัตร {visitCount} / บุคคล {visitorCount})";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SettingsViewModel> ToModelAsync(CompanyProfile c)
    {
        var opts = await _cloudOptions.GetAsync();
        var model = new SettingsViewModel
        {
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
        ApplyStatus(model);
        return model;
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
