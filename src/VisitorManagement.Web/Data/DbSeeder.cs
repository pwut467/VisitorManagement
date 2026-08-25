using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!await db.CompanyProfiles.AnyAsync())
        {
            db.CompanyProfiles.Add(new CompanyProfile
            {
                Name = "บริษัท ส.เขมราฐอินดัสตรี้ จำกัด (โรงโม่น้ำยืน)",
                Address = "199 หมู่ 9 บ.โนนทอง ต.สีวิเชียร อ.น้ำยืน จ.อุบลราชธานี",
                BadgeFooter = "กรุณาติดบัตรนี้ตลอดเวลาที่อยู่ในบริษัท และคืนบัตรเมื่อออกจากพื้นที่",
                DefaultVisitHours = 2,
                OverstayGraceMinutes = 15
            });
        }

        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Code = "HR", Name = "ทรัพยากรบุคคล" },
                new Department { Code = "IT", Name = "เทคโนโลยีสารสนเทศ" },
                new Department { Code = "OP", Name = "ปฏิบัติการ" },
                new Department { Code = "SA", Name = "ขายและการตลาด" },
                new Department { Code = "FN", Name = "การเงิน" });
            await db.SaveChangesAsync();
        }

        if (!await db.Gates.AnyAsync())
        {
            db.Gates.AddRange(
                new Gate { Name = "ประตูใหญ่", Location = "Lobby ชั้น 1" },
                new Gate { Name = "ประตูพนักงาน", Location = "ด้านข้างอาคาร" },
                new Gate { Name = "ประตูขนส่ง", Location = "Loading dock" });
        }

        if (!await db.VisitorTypes.AnyAsync())
        {
            db.VisitorTypes.AddRange(
                 new VisitorType { Name = "ผู้รับเหมา", BadgeLabel = "CONTRACTOR", Color = "#b45309", RequiresEscortDefault = true },
                new VisitorType { Name = "ลูกค้า / คู่ค้า", BadgeLabel = "GUEST", Color = "#1a56a0" },               
                new VisitorType { Name = "ส่งของ / ขนส่ง", BadgeLabel = "DELIVERY", Color = "#0f766e" },
                new VisitorType { Name = "สัมภาษณ์งาน", BadgeLabel = "INTERVIEW", Color = "#6d28d9" },
                new VisitorType { Name = "หน่วยงานราชการ", BadgeLabel = "OFFICIAL", Color = "#9f1239" },
                new VisitorType { Name = "อื่นๆ", BadgeLabel = "VISITOR", Color = "#334155" });
        }

        if (!await db.VisitPurposes.AnyAsync())
        {
            db.VisitPurposes.AddRange(
                 new VisitPurpose { Name = "ซื้อหิน" },
                new VisitPurpose { Name = "ประชุม / หารืองาน" },
                new VisitPurpose { Name = "ส่งเอกสาร / ส่งของ" },
                new VisitPurpose { Name = "ซ่อมบำรุง / ติดตั้ง" },
                new VisitPurpose { Name = "สัมภาษณ์งาน" },
                new VisitPurpose { Name = "เยี่ยมชมโรงงาน / สำนักงาน" },
                new VisitPurpose { Name = "อื่นๆ" });
        }

        await db.SaveChangesAsync();

        if (!await db.Employees.AnyAsync())
        {
            var hr = await db.Departments.FirstAsync(d => d.Code == "HR");
            var it = await db.Departments.FirstAsync(d => d.Code == "IT");
            var op = await db.Departments.FirstAsync(d => d.Code == "OP");
            var sa = await db.Departments.FirstAsync(d => d.Code == "SA");

            db.Employees.AddRange(
                new Employee { EmployeeCode = "5700530", FullName = "นายเวิน บุษภาค", DepartmentId = hr.Id, Phone = "081-111-0001", Email = "somchai@example.com" },
                new Employee { EmployeeCode = "5300162", FullName = "สมหญิง รักงาน", DepartmentId = it.Id, Phone = "081-111-0002", Email = "somying@example.com" },
                new Employee { EmployeeCode = "E003", FullName = "วิชัย รักษา", DepartmentId = op.Id, Phone = "081-111-0003", Email = "wichai@example.com" },
                new Employee { EmployeeCode = "E004", FullName = "นภา สายลม", DepartmentId = sa.Id, Phone = "081-111-0004", Email = "napa@example.com" },
                new Employee { EmployeeCode = "E005", FullName = "กิตติ ตั้งตรง", DepartmentId = it.Id, Phone = "081-111-0005", Email = "kitti@example.com" });
            await db.SaveChangesAsync();
        }

        await EnsureUserAsync(userManager, "admin@company.local", "Admin@12345", "ผู้ดูแลระบบ", AppRoles.Admin);
        await EnsureUserAsync(userManager, "security@company.local", "Security@12345", "รปภ. ประตูใหญ่", AppRoles.Security);
        await EnsureUserAsync(userManager, "reception@company.local", "Reception@12345", "เจ้าหน้าที่ต้อนรับ", AppRoles.Reception);

        var hostEmp = await db.Employees.FirstAsync(e => e.EmployeeCode == "E002");
        var hostUser = await EnsureUserAsync(userManager, "host@company.local", "Host@12345", hostEmp.FullName, AppRoles.Host);
        if (hostEmp.UserId != hostUser.Id)
        {
            hostEmp.UserId = hostUser.Id;
            await db.SaveChangesAsync();
        }

        if (!await db.BlacklistEntries.AnyAsync())
        {
            db.BlacklistEntries.Add(new BlacklistEntry
            {
                NationalId = "1234567890121",
                FullName = "นาย ไม่พึงประสงค์ ตัวอย่าง",
                Reason = "เคยฝ่าฝืนระเบียบความปลอดภัยของบริษัท",
                CreatedAt = TimeHelper.Now,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Visits.AnyAsync())
        {
            var visitor = new Visitor
            {
                NationalId = "3101700123452",
                Title = "นาย",
                FirstName = "ประเสริฐ",
                LastName = "มาเยือน",
                Phone = "089-111-2222",
                CompanyName = "บริษัท คู่ค้า จำกัด",
                CreatedAt = TimeHelper.Now,
                UpdatedAt = TimeHelper.Now
            };
            db.Visitors.Add(visitor);
            await db.SaveChangesAsync();

            var type = await db.VisitorTypes.FirstAsync();
            var purpose = await db.VisitPurposes.FirstAsync();
            var host = await db.Employees.FirstAsync();
            var gate = await db.Gates.FirstAsync();
            var now = TimeHelper.Now;

            db.Visits.Add(new Visit
            {
                VisitNumber = $"V{now:yyyyMMdd}-0001",
                VisitCode = Guid.NewGuid().ToString("N"),
                VisitorId = visitor.Id,
                VisitorTypeId = type.Id,
                VisitPurposeId = purpose.Id,
                HostEmployeeId = host.Id,
                GateInId = gate.Id,
                CompanyName = visitor.CompanyName,
                PurposeDetail = "ประชุมโครงการประจำเดือน",
                Status = VisitStatus.CheckedIn,
                CheckInAt = now.AddHours(-1),
                ExpectedCheckoutAt = now.AddHours(1),
                PdpaConsentAt = now.AddHours(-1),
                CreatedAt = now.AddHours(-1)
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
