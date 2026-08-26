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

    public SettingsController(AppDbContext db, ICloudVisitSyncService cloudSync, ICloudConnectionStatus cloudStatus)
    {
        _db = db;
        _cloudSync = cloudSync;
        _cloudStatus = cloudStatus;
    }

    public async Task<IActionResult> Index()
    {
        var c = await _db.CompanyProfiles.FirstAsync();
        return View(ToModel(c));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var current = await _db.CompanyProfiles.FirstAsync();
            model.CloudPasswordSet = !string.IsNullOrEmpty(current.CloudPassword);
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
        TempData["Success"] = "บันทึกการตั้งค่าแล้ว";
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
        TempData[ok ? "Success" : "Error"] = ok
            ? $"เชื่อมต่อ Cloud สำเร็จ — {snap.Server}/{snap.Database}"
            : $"เชื่อมต่อ Cloud ไม่สำเร็จ — {snap.LastError}";
        return RedirectToAction(nameof(Index));
    }

    private SettingsViewModel ToModel(CompanyProfile c)
    {
        var model = new SettingsViewModel
        {
            Name = c.Name,
            Address = c.Address,
            BadgeFooter = c.BadgeFooter,
            DefaultVisitHours = c.DefaultVisitHours,
            OverstayGraceMinutes = c.OverstayGraceMinutes,
            AutoPrintBadge = c.AutoPrintBadge,
            CloudEnabled = c.CloudEnabled,
            CloudServer = c.CloudServer,
            CloudDatabase = c.CloudDatabase,
            CloudUseWindowsAuth = c.CloudUseWindowsAuth,
            CloudUserId = c.CloudUserId,
            CloudPasswordSet = !string.IsNullOrEmpty(c.CloudPassword)
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
