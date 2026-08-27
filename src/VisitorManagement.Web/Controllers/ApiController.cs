using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Controllers;

[Authorize]
[Route("api")]
public class ApiController : Controller
{
    private readonly AppDbContext _db;
    private readonly IBlacklistService _blacklist;
    private readonly ICloudConnectionStatus _cloudStatus;
    private readonly ICompanyContext _companyContext;

    public ApiController(
        AppDbContext db,
        IBlacklistService blacklist,
        ICloudConnectionStatus cloudStatus,
        ICompanyContext companyContext)
    {
        _db = db;
        _blacklist = blacklist;
        _cloudStatus = cloudStatus;
        _companyContext = companyContext;
    }

    [HttpGet("visitors/by-national-id")]
    public async Task<IActionResult> VisitorByNationalId(string id)
    {
        var nationalId = ThaiNationalId.Normalize(id);
        if (nationalId.Length != 13)
        {
            return Json(new { found = false, valid = false });
        }

        var company = await _companyContext.GetActiveAsync();
        var blocked = await _blacklist.FindActiveAsync(nationalId, null);
        var visitor = await _db.Visitors.FirstOrDefaultAsync(v =>
            v.CompanyProfileId == company.Id && v.NationalId == nationalId);
        return Json(new
        {
            found = visitor is not null,
            valid = ThaiNationalId.IsValid(nationalId),
            checksumOk = ThaiNationalId.IsValid(nationalId),
            blocked = blocked is not null,
            blockReason = blocked?.Reason,
            visitor = visitor is null ? null : new
            {
                visitor.Title,
                visitor.FirstName,
                visitor.LastName,
                visitor.Phone,
                visitor.CompanyName,
                visitor.Address,
                visitor.PhotoPath,
                visitor.CardPhotoPath
            }
        });
    }

    [HttpGet("employees/search")]
    public async Task<IActionResult> Employees(string? q)
    {
        var query = _db.Employees.Where(e => e.IsActive).Include(e => e.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e => e.FullName.Contains(term) || e.EmployeeCode.Contains(term));
        }

        var list = await query.OrderBy(e => e.FullName).Take(20)
            .Select(e => new { e.Id, e.FullName, department = e.Department.Name, e.Phone })
            .ToListAsync();
        return Json(list);
    }

    [HttpGet("cloud/status")]
    public IActionResult CloudStatus()
    {
        var s = _cloudStatus.Current;
        return Json(new
        {
            enabled = s.Enabled,
            configured = s.Configured,
            online = s.Online,
            server = s.Server,
            database = s.Database,
            lastCheckedAt = s.LastCheckedAt?.ToString("dd/MM/yyyy HH:mm:ss"),
            lastError = s.LastError,
            pendingSyncCount = s.PendingSyncCount,
            label = !s.Enabled
                ? "Cloud ปิดใช้งาน"
                : !s.Configured
                    ? "Cloud ยังไม่ได้ตั้งค่า"
                    : s.Online
                        ? (s.PendingSyncCount > 0 ? $"Cloud ออนไลน์ · ค้างซิงก์ {s.PendingSyncCount}" : "Cloud ออนไลน์")
                        : "Cloud ออฟไลน์"
        });
    }
}
