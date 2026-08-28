using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Data;

public static class DbSeeder
{
    private static readonly string[] OfficialUserNames = ["SKAdmin", "9641"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetService<IConfiguration>();

        if (db.Database.IsSqlServer())
        {
            if (config is null)
            {
                throw new InvalidOperationException("IConfiguration is required to migrate SQL Server.");
            }

            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseBootstrap");
            var connectionResolver = scope.ServiceProvider.GetService<SqlConnectionResolver>();
            await DatabaseBootstrap.EnsureMigratedAsync(db, config, logger, connectionResolver);
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

        var cloudOpts = config is null ? new CloudOptions() : CloudOptions.FromConfiguration(config);

        if (!await db.CompanyProfiles.AnyAsync())
        {
            db.CompanyProfiles.Add(new CompanyProfile
            {
                CompanyCode = "SKNY",
                Name = "บริษัท ส.เขมราฐอินดัสตรี้ จำกัด (โรงโม่น้ำยืน)",
                Address = "199 หมู่ 9 บ.โนนทอง ต.สีวิเชียร อ.น้ำยืน จ.อุบลราชธานี",
                BadgeFooter = "กรุณาติดบัตรนี้ตลอดเวลาที่อยู่ในบริษัท และคืนบัตรเมื่อออกจากพื้นที่",
                DefaultVisitHours = 2,
                OverstayGraceMinutes = 15,
                AutoPrintBadge = true,
                IsActive = true,
                SeedRevision = 2,
                CloudEnabled = true,
                CloudServer = string.IsNullOrWhiteSpace(cloudOpts.Server) ? "192.168.11.204" : cloudOpts.Server,
                CloudDatabase = string.IsNullOrWhiteSpace(cloudOpts.Database) ? "VisitorManagment" : cloudOpts.Database,
                CloudUseWindowsAuth = cloudOpts.UseWindowsAuth,
                CloudUserId = cloudOpts.UserId,
                CloudPassword = cloudOpts.Password
            });
            await db.SaveChangesAsync();
        }
        else
        {
            var companies = await db.CompanyProfiles.ToListAsync();
            var dirty = false;
            if (!companies.Any(c => c.IsActive))
            {
                companies[0].IsActive = true;
                dirty = true;
            }

            foreach (var company in companies)
            {
                if (string.IsNullOrWhiteSpace(company.CompanyCode))
                {
                    company.CompanyCode = company.Id == companies[0].Id ? "SKNY" : $"C{company.Id}";
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(company.CloudServer))
                {
                    company.CloudEnabled = true;
                    company.CloudServer = string.IsNullOrWhiteSpace(cloudOpts.Server) ? "192.168.11.204" : cloudOpts.Server;
                    company.CloudDatabase = string.IsNullOrWhiteSpace(cloudOpts.Database) ? "VisitorManagment" : cloudOpts.Database;
                    company.CloudUseWindowsAuth = false;
                    dirty = true;
                }

                if (string.IsNullOrWhiteSpace(company.CloudUserId) && !string.IsNullOrWhiteSpace(cloudOpts.UserId))
                {
                    company.CloudUserId = cloudOpts.UserId;
                    dirty = true;
                }

                if (string.IsNullOrEmpty(company.CloudPassword) && !string.IsNullOrEmpty(cloudOpts.Password))
                {
                    company.CloudPassword = cloudOpts.Password;
                    dirty = true;
                }
            }

            if (dirty)
            {
                await db.SaveChangesAsync();
            }
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

        await ResetUsersAsync(db, userManager);
        await ClearVisitorRecordsOnceAsync(db);

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
    }

    private static async Task ResetUsersAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        const string defaultPassword = "123456";
        var keepNames = new HashSet<string>(OfficialUserNames, StringComparer.OrdinalIgnoreCase);
        var leftover = (await userManager.Users.ToListAsync())
            .Where(user => user.UserName is null || !keepNames.Contains(user.UserName))
            .ToList();

        if (leftover.Count > 0)
        {
            var leftoverIds = leftover.Select(user => user.Id).ToHashSet();
            foreach (var emp in await db.Employees.Where(e => e.UserId != null).ToListAsync())
            {
                if (emp.UserId is not null && leftoverIds.Contains(emp.UserId))
                {
                    emp.UserId = null;
                }
            }

            foreach (var visit in await db.Visits
                         .Where(v => v.RegisteredByUserId != null || v.CheckedOutByUserId != null)
                         .ToListAsync())
            {
                if (visit.RegisteredByUserId is not null && leftoverIds.Contains(visit.RegisteredByUserId))
                {
                    visit.RegisteredByUserId = null;
                }

                if (visit.CheckedOutByUserId is not null && leftoverIds.Contains(visit.CheckedOutByUserId))
                {
                    visit.CheckedOutByUserId = null;
                }
            }

            await db.SaveChangesAsync();

            foreach (var user in leftover)
            {
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        await EnsureUserAsync(userManager, "SKAdmin", defaultPassword, "ผู้ดูแลระบบ", AppRoles.Admin);
        await EnsureUserAsync(userManager, "9641", defaultPassword, "รปภ.", AppRoles.Security);
    }

    private static async Task ClearVisitorRecordsOnceAsync(AppDbContext db)
    {
        const int targetRevision = 2;
        var company = await db.CompanyProfiles.FirstOrDefaultAsync();
        if (company is null || company.SeedRevision >= targetRevision)
        {
            return;
        }

        await ClearAllVisitorDataAsync(db);
        company.SeedRevision = targetRevision;
        await db.SaveChangesAsync();
    }

    public static async Task ClearAllVisitorDataAsync(AppDbContext db)
    {
        db.VisitItems.RemoveRange(await db.VisitItems.ToListAsync());
        db.Visits.RemoveRange(await db.Visits.ToListAsync());
        db.Visitors.RemoveRange(await db.Visitors.ToListAsync());
        await db.SaveChangesAsync();
    }

    public static async Task ClearVisitorDataForCompanyAsync(AppDbContext db, int companyProfileId)
    {
        var visitIds = await db.Visits
            .Where(v => v.CompanyProfileId == companyProfileId)
            .Select(v => v.Id)
            .ToListAsync();
        if (visitIds.Count > 0)
        {
            db.VisitItems.RemoveRange(await db.VisitItems.Where(i => visitIds.Contains(i.VisitId)).ToListAsync());
            db.Visits.RemoveRange(await db.Visits.Where(v => v.CompanyProfileId == companyProfileId).ToListAsync());
        }

        db.Visitors.RemoveRange(await db.Visitors.Where(v => v.CompanyProfileId == companyProfileId).ToListAsync());
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = null,
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
        else
        {
            user.FullName = fullName;
            user.IsActive = true;
            await userManager.UpdateAsync(user);
        }

        await EnsureExclusiveRoleAsync(userManager, user, role);
    }

    /// <summary>
    /// Ensures the user has exactly <paramref name="role"/> (e.g. 9641 = Security only).
    /// </summary>
    public static async Task EnsureExclusiveRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string role)
    {
        var current = await userManager.GetRolesAsync(user);
        var extra = current.Where(r => !string.Equals(r, role, StringComparison.OrdinalIgnoreCase)).ToList();
        if (extra.Count > 0)
        {
            var remove = await userManager.RemoveFromRolesAsync(user, extra);
            if (!remove.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", remove.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var add = await userManager.AddToRoleAsync(user, role);
            if (!add.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", add.Errors.Select(e => e.Description)));
            }
        }
    }
}