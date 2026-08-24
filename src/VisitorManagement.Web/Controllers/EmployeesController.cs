using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class EmployeesController : Controller
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Employees.Include(e => e.Department).OrderBy(e => e.EmployeeCode).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await DepartmentsAsync();
        return View(new EmployeeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        await DepartmentsAsync();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _db.Employees.Add(new Employee
        {
            EmployeeCode = model.EmployeeCode.Trim(),
            FullName = model.FullName.Trim(),
            DepartmentId = model.DepartmentId,
            Phone = model.Phone,
            Email = model.Email,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "เพิ่มพนักงานแล้ว";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null)
        {
            return NotFound();
        }

        await DepartmentsAsync();
        return View(new EmployeeFormViewModel
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FullName = e.FullName,
            DepartmentId = e.DepartmentId,
            Phone = e.Phone,
            Email = e.Email,
            IsActive = e.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        await DepartmentsAsync();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var e = await _db.Employees.FindAsync(id);
        if (e is null)
        {
            return NotFound();
        }

        e.EmployeeCode = model.EmployeeCode.Trim();
        e.FullName = model.FullName.Trim();
        e.DepartmentId = model.DepartmentId;
        e.Phone = model.Phone;
        e.Email = model.Email;
        e.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "บันทึกพนักงานแล้ว";
        return RedirectToAction(nameof(Index));
    }

    private async Task DepartmentsAsync()
    {
        ViewBag.Departments = await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
            .ToListAsync();
    }
}
