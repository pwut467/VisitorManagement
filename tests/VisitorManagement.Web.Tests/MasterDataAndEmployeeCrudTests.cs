using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Controllers;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Tests;

public class MasterDataAndEmployeeCrudTests
{
    [Fact]
    public async Task MasterData_CanEditAndDeleteUnusedRecords()
    {
        var db = CreateDb();
        db.Departments.Add(new Department { Code = "TMP", Name = "ชั่วคราว" });
        db.Gates.Add(new Gate { Name = "ประตูทดสอบ", Location = "A" });
        db.VisitorTypes.Add(new VisitorType { Name = "ทดสอบ", BadgeLabel = "TEST", Color = "#000000" });
        db.VisitPurposes.Add(new VisitPurpose { Name = "ทดสอบวัตถุประสงค์" });
        await db.SaveChangesAsync();

        var controller = CreateMasterDataController(db);
        var dept = await db.Departments.SingleAsync(d => d.Code == "TMP");
        var gate = await db.Gates.SingleAsync(g => g.Name == "ประตูทดสอบ");
        var type = await db.VisitorTypes.SingleAsync(t => t.BadgeLabel == "TEST");
        var purpose = await db.VisitPurposes.SingleAsync(p => p.Name == "ทดสอบวัตถุประสงค์");

        Assert.IsType<RedirectToActionResult>(await controller.EditDepartment(dept.Id, "TMP2", "ชั่วคราว2"));
        Assert.Equal("TMP2", (await db.Departments.FindAsync(dept.Id))!.Code);

        Assert.IsType<RedirectToActionResult>(await controller.EditGate(gate.Id, "ประตูใหม่", "B"));
        Assert.Equal("ประตูใหม่", (await db.Gates.FindAsync(gate.Id))!.Name);

        Assert.IsType<RedirectToActionResult>(await controller.EditType(type.Id, "ประเภทใหม่", "NEW", "#111111"));
        Assert.Equal("NEW", (await db.VisitorTypes.FindAsync(type.Id))!.BadgeLabel);

        Assert.IsType<RedirectToActionResult>(await controller.EditPurpose(purpose.Id, "วัตถุประสงค์ใหม่"));
        Assert.Equal("วัตถุประสงค์ใหม่", (await db.VisitPurposes.FindAsync(purpose.Id))!.Name);

        Assert.IsType<RedirectToActionResult>(await controller.Delete("department", dept.Id));
        Assert.IsType<RedirectToActionResult>(await controller.Delete("gate", gate.Id));
        Assert.IsType<RedirectToActionResult>(await controller.Delete("type", type.Id));
        Assert.IsType<RedirectToActionResult>(await controller.Delete("purpose", purpose.Id));

        Assert.Empty(db.Departments.Where(d => d.Id == dept.Id));
        Assert.Empty(db.Gates.Where(g => g.Id == gate.Id));
        Assert.Empty(db.VisitorTypes.Where(t => t.Id == type.Id));
        Assert.Empty(db.VisitPurposes.Where(p => p.Id == purpose.Id));
    }

    [Fact]
    public async Task MasterData_DeleteDepartment_BlockedWhenEmployeesExist()
    {
        var db = CreateDb();
        var dept = new Department { Code = "HR", Name = "HR" };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();
        db.Employees.Add(new Employee { EmployeeCode = "E1", FullName = "A", DepartmentId = dept.Id });
        await db.SaveChangesAsync();

        var controller = CreateMasterDataController(db);
        await controller.Delete("department", dept.Id);
        Assert.NotNull(await db.Departments.FindAsync(dept.Id));
        Assert.Contains("พนักงาน", controller.TempData["Error"]?.ToString());
    }

    [Fact]
    public async Task Employees_CanEditAndDeleteWhenUnused()
    {
        var db = CreateDb();
        var dept = new Department { Code = "IT", Name = "IT" };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();
        db.Employees.Add(new Employee
        {
            EmployeeCode = "E100",
            FullName = "เดิม",
            DepartmentId = dept.Id,
            Phone = "000",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var emp = await db.Employees.SingleAsync(e => e.EmployeeCode == "E100");

        var controller = CreateEmployeesController(db);
        var edit = await controller.Edit(emp.Id, new EmployeeFormViewModel
        {
            Id = emp.Id,
            EmployeeCode = "E100",
            FullName = "ใหม่",
            DepartmentId = dept.Id,
            Phone = "111",
            IsActive = true
        });
        Assert.IsType<RedirectToActionResult>(edit);
        Assert.Equal("ใหม่", (await db.Employees.FindAsync(emp.Id))!.FullName);

        var delete = await controller.Delete(emp.Id);
        Assert.IsType<RedirectToActionResult>(delete);
        Assert.Null(await db.Employees.FindAsync(emp.Id));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static MasterDataController CreateMasterDataController(AppDbContext db)
    {
        return new MasterDataController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider())
        };
    }

    private static EmployeesController CreateEmployeesController(AppDbContext db)
    {
        return new EmployeesController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider())
        };
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _data = new();
        public IDictionary<string, object?> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) =>
            _data = new Dictionary<string, object?>(values);
    }
}
