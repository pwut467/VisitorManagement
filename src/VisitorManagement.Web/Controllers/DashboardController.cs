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
    private readonly ICompanyContext _companyContext;

    public DashboardController(AppDbContext db, ICompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetActiveAsync();
        var now = TimeHelper.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var visits = _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee)
            .Include(v => v.VisitorType)
            .Where(v => v.CompanyProfileId == company.Id);

        var onSite = await visits.Where(v => v.Status == VisitStatus.CheckedIn)
            .OrderBy(v => v.CheckInAt)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            OnSiteCount = onSite.Count,
            TodayCheckIns = await visits.CountAsync(v => v.CheckInAt >= today && v.CheckInAt < tomorrow),
            TodayCheckOuts = await visits.CountAsync(v => v.CheckOutAt >= today && v.CheckOutAt < tomorrow),
            OnSite = onSite.Take(8).ToList()
        };

        var todayIns = await visits
            .Where(v => v.CheckInAt >= today && v.CheckInAt < tomorrow)
            .Select(v => v.CheckInAt)
            .ToListAsync();
        vm.Hourly = Enumerable.Range(7, 13).Select(h => new HourlyPoint
        {
            Label = $"{h:00}",
            Count = todayIns.Count(t => t!.Value.Hour == h)
        }).ToList();

        ViewBag.ActiveCompany = company;
        return View(vm);
    }
}
