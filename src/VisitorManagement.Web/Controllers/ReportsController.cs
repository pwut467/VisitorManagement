using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.FrontDesk)]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? visitorTypeId)
    {
        var start = (from ?? TimeHelper.Today).Date;
        var end = (to ?? TimeHelper.Today).Date.AddDays(1);
        var list = await QueryVisits(start, end, visitorTypeId).ToListAsync();
        ViewBag.From = start;
        ViewBag.To = end.AddDays(-1);
        ViewBag.Types = await _db.VisitorTypes.OrderBy(x => x.Name).ToListAsync();
        ViewBag.VisitorTypeId = visitorTypeId;
        return View(list);
    }

    public async Task<IActionResult> Excel(DateTime? from, DateTime? to, int? visitorTypeId)
    {
        var start = (from ?? TimeHelper.Today).Date;
        var end = (to ?? TimeHelper.Today).Date.AddDays(1);
        var list = await QueryVisits(start, end, visitorTypeId).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ผู้มาติดต่อ");
        var headers = new[] { "ลำดับ", "รหัส", "ชื่อผู้มาติดต่อ", "ทะเบียนรถ", "เลขบัตร", "หน่วยงาน", "ประเภท", "วัตถุประสงค์", "ผู้ต้องการพบ", "เข้า", "ออก", "สถานะ" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        var seq = 1;
        foreach (var v in list)
        {
            sheet.Cell(row, 1).Value = seq++;
            sheet.Cell(row, 2).Value = v.VisitNumber;
            sheet.Cell(row, 3).Value = v.GuestFullName;
            sheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(v.VehiclePlate) ? "" : v.VehiclePlate;
            sheet.Cell(row, 5).Value = ThaiNationalId.Mask(v.Visitor.NationalId);
            sheet.Cell(row, 6).Value = v.CompanyName ?? "";
            sheet.Cell(row, 7).Value = v.VisitorType.Name;
            sheet.Cell(row, 8).Value = v.VisitPurpose.Name;
            sheet.Cell(row, 9).Value = v.HostEmployee.FullName;
            sheet.Cell(row, 10).Value = v.CheckInAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            sheet.Cell(row, 11).Value = v.CheckOutAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            sheet.Cell(row, 12).Value = v.Status.ToString();
            row++;
        }

        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a56a0");
        header.Style.Font.FontColor = XLColor.White;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var toDate = end.AddDays(-1);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"visitors-{start:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx");
    }

    private IQueryable<Visit> QueryVisits(DateTime start, DateTime end, int? visitorTypeId)
    {
        var q = _db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.HostEmployee)
            .Include(v => v.VisitorType)
            .Include(v => v.VisitPurpose)
            .Where(v => (v.CheckInAt ?? v.CreatedAt) >= start && (v.CheckInAt ?? v.CreatedAt) < end);

        if (visitorTypeId is int t)
        {
            q = q.Where(v => v.VisitorTypeId == t);
        }

        return q.OrderBy(v => v.CheckInAt);
    }
}
