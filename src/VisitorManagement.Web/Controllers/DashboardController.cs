using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = TimeHelper.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var visits = _db.Visits.Include(v => v.Visitor).Include(v => v.HostEmployee).Include(v => v.VisitorType);

        var onSite = await visits.Where(v => v.Status == VisitStatus.CheckedIn)
            .OrderBy(v => v.CheckInAt)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            OnSiteCount = onSite.Count,
            TodayCheckIns = await visits.CountAsync(v => v.CheckInAt >= today && v.CheckInAt < tomorrow),
            TodayCheckOuts = await visits.CountAsync(v => v.CheckOutAt >= today && v.CheckOutAt < tomorrow),
            PendingPreReg = await visits.CountAsync(v => v.Status == VisitStatus.PreRegistered && (v.AppointmentAt == null || v.AppointmentAt >= today)),
            OnSite = onSite.Take(8).ToList(),
            Overstay = onSite.Where(v => v.IsOverstay(now)).ToList(),
            Recent = await visits.OrderByDescending(v => v.CreatedAt).Take(8).ToListAsync()
        };
        vm.OverstayCount = vm.Overstay.Count;

        var todayIns = await _db.Visits
            .Where(v => v.CheckInAt >= today && v.CheckInAt < tomorrow)
            .Select(v => v.CheckInAt)
            .ToListAsync();
        vm.Hourly = Enumerable.Range(7, 13).Select(h => new HourlyPoint
        {
            Label = $"{h:00}",
            Count = todayIns.Count(t => t!.Value.Hour == h)
        }).ToList();

        return View(vm);
    }
}
