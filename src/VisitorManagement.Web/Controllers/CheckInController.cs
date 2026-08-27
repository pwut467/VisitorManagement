using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class CheckInController : Controller
{
    private readonly AppDbContext _db;
    private readonly IVisitRegistrationService _registration;
    private readonly IConfiguration _config;
    private readonly ICompanyContext _companyContext;

    public CheckInController(
        AppDbContext db,
        IVisitRegistrationService registration,
        IConfiguration config,
        ICompanyContext companyContext)
    {
        _db = db;
        _registration = registration;
        _config = config;
        _companyContext = companyContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? visitId)
    {
        var model = new CheckInViewModel();
        if (visitId is int id)
        {
            var visit = await _db.Visits
                .Include(v => v.Visitor)
                .Include(v => v.HostEmployee)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (visit is not null)
            {
                model.VisitId = visit.Id;
                model.NationalId = visit.Visitor.NationalId;
                model.Title = !string.IsNullOrWhiteSpace(visit.GuestTitle) ? visit.GuestTitle : visit.Visitor.Title;
                model.FirstName = !string.IsNullOrWhiteSpace(visit.GuestFirstName) ? visit.GuestFirstName : visit.Visitor.FirstName;
                model.LastName = !string.IsNullOrWhiteSpace(visit.GuestLastName) ? visit.GuestLastName : visit.Visitor.LastName;
                model.Phone = visit.GuestPhone ?? visit.Visitor.Phone ?? "";
                model.CompanyName = visit.CompanyName ?? visit.Visitor.CompanyName;
                model.Address = visit.Visitor.Address;
                model.VisitorTypeId = visit.VisitorTypeId;
                model.VisitPurposeId = visit.VisitPurposeId;
                model.PurposeDetail = visit.PurposeDetail;
                model.HostName = visit.HostEmployee.FullName;
                model.VehiclePlate = visit.VehiclePlate ?? "";
                model.VehicleType = visit.VehicleType ?? "";
                model.AccompanyingCount = visit.AccompanyingCount;
                model.Notes = visit.Notes;
                model.PdpaConsent = true;
            }
        }

        await PopulateAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckInViewModel model)
    {
        await PopulateAsync(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (User.IsInRole(AppRoles.Host) && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Security) && !User.IsInRole(AppRoles.Reception))
        {
            model.SubmitAction = "preregister";
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var result = await _registration.RegisterAsync(model, userId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            if (result.Blacklist is not null)
            {
                TempData["Error"] = "ปฏิเสธการเข้าพื้นที่ — " + result.Error;
            }
            return View(model);
        }

        TempData["Success"] = model.SubmitAction == "preregister"
            ? $"ลงทะเบียนล่วงหน้า {result.Visit!.VisitNumber} เรียบร้อย"
            : $"Check-in {result.Visit!.VisitNumber} เรียบร้อย";

        if (model.SubmitAction == "preregister")
        {
            return RedirectToAction("Details", "Visits", new { id = result.Visit!.Id, returnUrl = Url.Action(nameof(Index)) });
        }

        var activeCompany = await _companyContext.GetActiveAsync();
        if (activeCompany.AutoPrintBadge)
        {
            TempData["PrintBadgeUrl"] = Url.Action("Badge", "Visits", new { id = result.Visit!.Id, autoprint = "true" });
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction("Badge", "Visits", new { id = result.Visit!.Id, autoprint = "false" });
    }

    private async Task PopulateAsync(CheckInViewModel model)
    {
        var company = await _companyContext.GetActiveAsync();
        if (model.ExpectedHours <= 0)
        {
            model.ExpectedHours = company.DefaultVisitHours > 0 ? company.DefaultVisitHours : 2;
        }

        ViewBag.AutoPrintBadge = company.AutoPrintBadge;
        ViewBag.ActiveCompany = company;

        model.Titles = new[] { "นาย", "นาง", "นางสาว", "อื่นๆ" }
            .Select(t => new SelectListItem(t, t, t == model.Title));
        model.VisitorTypes = new[] { new SelectListItem("— เลือก —", "") }
            .Concat(await _db.VisitorTypes.Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name, Selected = x.Id == model.VisitorTypeId })
                .ToListAsync());
        model.VisitPurposes = new[] { new SelectListItem("— เลือก —", "") }
            .Concat(await _db.VisitPurposes.Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name, Selected = x.Id == model.VisitPurposeId })
                .ToListAsync());
        model.VehicleTypes = new[] { "", "รถยนต์", "รถกระบะ", "รถจักรยานยนต์", "รถตู้", "รถ 6ล้อ", "รถบรรทุก 10 ล้อ", "รถพ่วง" }
            .Select(t => new SelectListItem(string.IsNullOrEmpty(t) ? "— เลือก —" : t, t, t == (model.VehicleType ?? "")));
        ViewBag.CardReaderUrl = _config["CardReader:AgentUrl"] ?? "http://127.0.0.1:5001";
    }
}
