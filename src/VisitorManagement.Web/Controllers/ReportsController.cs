using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.FrontDesk)]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? visitorTypeId, int? departmentId)
    {
        var start = (from ?? TimeHelper.Today).Date;
        var end = (to ?? TimeHelper.Today).Date.AddDays(1);

        var q = _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Include(v => v.VisitPurpose)
            .Include(v => v.GateIn)
            .Where(v => (v.CheckInAt ?? v.CreatedAt) >= start && (v.CheckInAt ?? v.CreatedAt) < end);

        if (visitorTypeId is int t)
        {
            q = q.Where(v => v.VisitorTypeId == t);
        }

        if (departmentId is int d)
        {
            q = q.Where(v => v.HostEmployee.DepartmentId == d);
        }

        var list = await q.OrderBy(v => v.CheckInAt).ToListAsync();
        ViewBag.From = start;
        ViewBag.To = end.AddDays(-1);
        ViewBag.Types = await _db.VisitorTypes.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Departments = await _db.Departments.OrderBy(x => x.Name).ToListAsync();
        ViewBag.VisitorTypeId = visitorTypeId;
        ViewBag.DepartmentId = departmentId;
        return View(list);
    }

    public async Task<IActionResult> Csv(DateTime? from, DateTime? to)
    {
        var start = (from ?? TimeHelper.Today).Date;
        var end = (to ?? TimeHelper.Today).Date.AddDays(1);
        var list = await _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.VisitorType)
            .Where(v => (v.CheckInAt ?? v.CreatedAt) >= start && (v.CheckInAt ?? v.CreatedAt) < end)
            .OrderBy(v => v.CheckInAt)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("VisitNumber,Name,NationalId,Company,Type,Host,Department,CheckIn,CheckOut,Vehicle,Status");
        foreach (var v in list)
        {
            sb.AppendLine(string.Join(',',
                Csv(v.VisitNumber),
                Csv(v.Visitor.FullName),
                Csv(ThaiNationalId.Mask(v.Visitor.NationalId)),
                Csv(v.CompanyName),
                Csv(v.VisitorType.Name),
                Csv(v.HostEmployee.FullName),
                Csv(v.HostEmployee.Department.Name),
                Csv(v.CheckInAt?.ToString("yyyy-MM-dd HH:mm")),
                Csv(v.CheckOutAt?.ToString("yyyy-MM-dd HH:mm")),
                Csv(v.VehiclePlate),
                Csv(v.Status.ToString())));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"visitors-{start:yyyyMMdd}.csv");
    }

    private static string Csv(string? value)
    {
        var s = value ?? "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
        {
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        return s;
    }
}
