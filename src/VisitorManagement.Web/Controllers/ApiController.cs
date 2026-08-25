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

    public ApiController(AppDbContext db, IBlacklistService blacklist)
    {
        _db = db;
        _blacklist = blacklist;
    }

    [HttpGet("visitors/by-national-id")]
    public async Task<IActionResult> VisitorByNationalId(string id)
    {
        var nationalId = ThaiNationalId.Normalize(id);
        if (nationalId.Length != 13)
        {
            return Json(new { found = false, valid = false });
        }

        var blocked = await _blacklist.FindActiveAsync(nationalId, null);
        var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.NationalId == nationalId);
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
                visitor.Email,
                visitor.CompanyName,
                visitor.Address,
                visitor.PhotoPath
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

}
