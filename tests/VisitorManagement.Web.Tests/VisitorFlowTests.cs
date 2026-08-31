using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Services;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Tests;

public class ThaiNationalIdTests
{
    [Theory]
    [InlineData("3101700123452", true)]
    [InlineData("1103700156780", true)]
    [InlineData("1234567890121", true)]
    [InlineData("1234567890123", false)]
    [InlineData("123", false)]
    [InlineData("310170012345a", false)]
    public void ValidatesChecksum(string id, bool expected)
    {
        Assert.Equal(expected, ThaiNationalId.IsValid(id));
    }

    [Fact]
    public void NormalizesAndMasks()
    {
        Assert.Equal("3101700123452", ThaiNationalId.Normalize("3-1017-00123-45-2"));
        Assert.Equal("3-xxxx-xxxxx-45-2", ThaiNationalId.Mask("3101700123452"));
        Assert.Equal("1-xxxx-xxxxx-78-0", ThaiNationalId.Mask("1103700156780"));
        Assert.Equal("3-xxxx-xxxxx-45-2", ThaiNationalId.Mask("3-1017-00123-45-2"));
        Assert.Equal("3-1017-00123-45-2", ThaiNationalId.Format("3101700123452"));
        Assert.Equal("1-1037-00156-78-0", ThaiNationalId.Format("1103700156780"));
    }
}

public class VisitNumberServiceTests
{
    [Fact]
    public async Task SequencesPerDayAndCompany()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var company = db.CompanyProfiles.Single();
        var svc = new VisitNumberService(db);
        var day = new DateTime(2026, 8, 24);
        var first = await svc.NextAsync(day, company.Id, company.CompanyCode);
        db.Visits.Add(MinimalVisit(db, first, company));
        await db.SaveChangesAsync();
        var second = await svc.NextAsync(day, company.Id, company.CompanyCode);
        Assert.Equal("SKNY-V20260824-0001", first);
        Assert.Equal("SKNY-V20260824-0002", second);
    }

    private static Visit MinimalVisit(AppDbContext db, string number, CompanyProfile company)
    {
        var dept = db.Departments.First();
        var emp = db.Employees.First();
        var type = db.VisitorTypes.First();
        var purpose = db.VisitPurposes.First();
        var visitor = new Visitor
        {
            CompanyProfileId = company.Id,
            NationalId = "3101700123452",
            FirstName = "A",
            LastName = "B",
            CreatedAt = TimeHelper.Now,
            UpdatedAt = TimeHelper.Now
        };
        return new Visit
        {
            CompanyProfileId = company.Id,
            HostCompanyCode = company.CompanyCode,
            VisitNumber = number,
            VisitCode = Guid.NewGuid().ToString("N"),
            Visitor = visitor,
            VisitorType = type,
            VisitPurpose = purpose,
            HostEmployee = emp,
            Status = VisitStatus.CheckedIn,
            CreatedAt = TimeHelper.Now
        };
    }
}

public class VisitRegistrationServiceTests
{
    [Fact]
    public async Task StoresCardAndWebcamPhotosSeparately()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var photos = new TestDb.RecordingPhotos();
        var svc = TestDb.CreateRegistration(db, photos);

        var model = TestDb.ValidCheckIn(db);
        model.PhotoDataUrl = "data:image/jpeg;base64,webcam";
        model.CardPhotoDataUrl = "data:image/jpeg;base64,card";

        var result = await svc.RegisterAsync(model, "user-1");
        Assert.True(result.Succeeded);
        var visit = await db.Visits.Include(v => v.Visitor).SingleAsync();
        Assert.Equal("/uploads/photos/" + visit.VisitCode + "-webcam.jpg", visit.PhotoPath);
        Assert.Equal("/uploads/photos/" + visit.VisitCode + "-card.jpg", visit.CardPhotoPath);
        Assert.Equal(visit.PhotoPath, visit.Visitor.PhotoPath);
        Assert.Equal(visit.CardPhotoPath, visit.Visitor.CardPhotoPath);
        Assert.Contains(visit.VisitCode + "-webcam", photos.SavedStems);
        Assert.Contains(visit.VisitCode + "-card", photos.SavedStems);
    }

    [Fact]
    public async Task CheckInThenCheckOut()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);

        var model = TestDb.ValidCheckIn(db);
        model.Address = "99 ถนนทดสอบ เขตคลองเตย กรุงเทพฯ";
        var result = await svc.RegisterAsync(model, "user-1");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(VisitStatus.CheckedIn, result.Visit!.Status);
        var stored = await db.Visitors.SingleAsync();
        Assert.Equal("99 ถนนทดสอบ เขตคลองเตย กรุงเทพฯ", stored.Address);
        Assert.Null(stored.Email);
        Assert.Null(stored.DateOfBirth);
        Assert.NotNull(result.Visit.CheckInAt);
        Assert.StartsWith("SKNY-V", result.Visit.VisitNumber);

        var outResult = await svc.CheckOutAsync(result.Visit.VisitCode, db.Gates.First().Id, "user-1", null);
        Assert.True(outResult.Succeeded, outResult.Error);
        Assert.Equal(VisitStatus.CheckedOut, outResult.Visit!.Status);
        Assert.NotNull(outResult.Visit.CheckOutAt);
    }

    [Fact]
    public async Task AcceptsVisitPrefixOnQrPayload()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var checkIn = await svc.RegisterAsync(TestDb.ValidCheckIn(db), "user-1");
        var outResult = await svc.CheckOutAsync("VISIT|" + checkIn.Visit!.VisitCode, null, "user-1", "คืนบัตร");
        Assert.True(outResult.Succeeded, outResult.Error);
    }

    [Fact]
    public async Task BlocksBlacklistedNationalId()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.NationalId = "1234567890121";
        model.FirstName = "ไม่พึงประสงค์";
        var result = await svc.RegisterAsync(model, "user-1");
        Assert.False(result.Succeeded);
        Assert.Contains("บัญชีดำ", result.Error);
        Assert.NotNull(result.Blacklist);
    }

    [Fact]
    public async Task RequiresPdpaForCheckIn()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.PdpaConsent = false;
        var result = await svc.RegisterAsync(model, "user-1");
        Assert.False(result.Succeeded);
        Assert.Contains("PDPA", result.Error);
    }

    [Fact]
    public void PdpaConsentDefaultsToChecked()
    {
        Assert.True(new CheckInViewModel().PdpaConsent);
    }

    [Fact]
    public async Task RejectsInvalidNationalId()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.NationalId = "1111111111111";
        var result = await svc.RegisterAsync(model, "user-1");
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PreRegisterDoesNotSetCheckInTime()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.SubmitAction = "preregister";
        model.PdpaConsent = false;
        var result = await svc.RegisterAsync(model, "host-1");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(VisitStatus.PreRegistered, result.Visit!.Status);
        Assert.Null(result.Visit.CheckInAt);
    }

    [Fact]
    public async Task ReturningVisitorKeepsMasterName_StoresVisitName()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var first = TestDb.ValidCheckIn(db);
        first.FirstName = "สมชาย";
        first.LastName = "ใจดี";
        first.Phone = "081-111-1111";
        await svc.RegisterAsync(first, "u1");

        var again = TestDb.ValidCheckIn(db);
        again.Phone = "089-000-1111";
        again.FirstName = "ทดลอง";
        again.LastName = "ผิดชื่อ";
        var second = await svc.RegisterAsync(again, "u1");
        Assert.True(second.Succeeded, second.Error);
        Assert.Equal(1, await db.Visitors.CountAsync());
        Assert.Equal(2, await db.Visits.CountAsync());

        var master = db.Visitors.Single();
        Assert.Equal("สมชาย", master.FirstName);
        Assert.Equal("ใจดี", master.LastName);
        Assert.Equal("081-111-1111", master.Phone);

        var visit = await db.Visits.OrderByDescending(v => v.Id).FirstAsync();
        Assert.Equal("ทดลอง", visit.GuestFirstName);
        Assert.Equal("ผิดชื่อ", visit.GuestLastName);
        Assert.Equal("089-000-1111", visit.GuestPhone);
        Assert.Equal("ทดลอง ผิดชื่อ", $"{visit.GuestFirstName} {visit.GuestLastName}".Trim());
        Assert.Contains("ทดลอง", visit.GuestFullName);
        Assert.True(visit.HasNameMismatchWithMaster);
        Assert.Equal(master.CompanyProfileId, visit.CompanyProfileId);
    }

    [Fact]
    public async Task SameNationalIdCanExistInDifferentCompanies()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        await svc.RegisterAsync(TestDb.ValidCheckIn(db), "u1");

        var other = new CompanyProfile
        {
            CompanyCode = "SITE2",
            Name = "บริษัทสอง",
            IsActive = true,
            SeedRevision = 2
        };
        db.CompanyProfiles.Add(other);
        foreach (var c in db.CompanyProfiles.Where(c => c.CompanyCode != "SITE2"))
        {
            c.IsActive = false;
        }
        await db.SaveChangesAsync();

        var again = TestDb.ValidCheckIn(db);
        again.FirstName = "คนละบริษัท";
        var result = await svc.RegisterAsync(again, "u1");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(2, await db.Visitors.CountAsync());
        Assert.Equal(2, await db.Visits.CountAsync());
        Assert.Contains(db.Visits, v => v.HostCompanyCode == "SKNY");
        Assert.Contains(db.Visits, v => v.HostCompanyCode == "SITE2");
    }

    [Fact]
    public async Task PhoneIsOptional()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.Phone = null;
        var result = await svc.RegisterAsync(model, "u1");
        Assert.True(result.Succeeded, result.Error);
        Assert.True(string.IsNullOrEmpty(db.Visitors.Single().Phone));
    }

    [Fact]
    public async Task TypedHostNameReusesExistingEmployee()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.HostName = "  สมหญิง   รักงาน  ";
        var result = await svc.RegisterAsync(model, "u1");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(1, await db.Employees.CountAsync());
        Assert.Equal(db.Employees.Single().Id, result.Visit!.HostEmployeeId);
        Assert.Equal("สมหญิง รักงาน", db.Employees.Single().FullName);
    }

    [Fact]
    public async Task TypedHostNameCreatesEmployeeWhenUnknown()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.HostName = "นายเวิน บุษภาค";
        var result = await svc.RegisterAsync(model, "u1");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(2, await db.Employees.CountAsync());
        Assert.Equal("นายเวิน บุษภาค", result.Visit!.HostEmployee.FullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("stale-user-id")]
    public async Task UnknownOrEmptyUserIdDoesNotBlockCheckIn(string userId)
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var result = await svc.RegisterAsync(TestDb.ValidCheckIn(db), userId);
        Assert.True(result.Succeeded, result.Error);
        Assert.Null(result.Visit!.RegisteredByUserId);
    }

    [Fact]
    public async Task ExistingUserIdIsStoredOnCheckInAndCheckOut()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        db.Users.Add(new ApplicationUser
        {
            Id = "user-real",
            UserName = "SKAdmin",
            FullName = "ผู้ดูแลระบบ",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var svc = TestDb.CreateRegistration(db);

        var result = await svc.RegisterAsync(TestDb.ValidCheckIn(db), "user-real");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("user-real", result.Visit!.RegisteredByUserId);

        var outResult = await svc.CheckOutAsync(result.Visit.VisitCode, db.Gates.First().Id, "gone-user", null);
        Assert.True(outResult.Succeeded, outResult.Error);
        Assert.Null(outResult.Visit!.CheckedOutByUserId);
    }

    [Fact]
    public async Task EmptyHostNameIsRejected()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var model = TestDb.ValidCheckIn(db);
        model.HostName = "   ";
        var result = await svc.RegisterAsync(model, "u1");
        Assert.False(result.Succeeded);
        Assert.Contains("พนักงาน", result.Error);
    }

    [Fact]
    public async Task VehicleTypeAndPlateAreRequired()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        var missingType = TestDb.ValidCheckIn(db);
        missingType.VehicleType = "  ";
        var typeResult = await svc.RegisterAsync(missingType, "u1");
        Assert.False(typeResult.Succeeded);
        Assert.Contains("ประเภทรถ", typeResult.Error);

        var missingPlate = TestDb.ValidCheckIn(db);
        missingPlate.VehiclePlate = "";
        var plateResult = await svc.RegisterAsync(missingPlate, "u1");
        Assert.False(plateResult.Succeeded);
        Assert.Contains("ทะเบียนรถ", plateResult.Error);
    }
}

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static async Task SeedGraphAsync(AppDbContext db)
    {
        db.CompanyProfiles.Add(new CompanyProfile
        {
            CompanyCode = "SKNY",
            Name = "บริษัททดสอบ",
            IsActive = true,
            SeedRevision = 2,
            DefaultVisitHours = 2,
            AutoPrintBadge = true
        });
        var dept = new Department { Code = "IT", Name = "ไอที" };
        db.Departments.Add(dept);
        db.Employees.Add(new Employee { EmployeeCode = "E1", FullName = "สมหญิง รักงาน", Department = dept });
        db.VisitorTypes.Add(new VisitorType { Name = "ลูกค้า", BadgeLabel = "GUEST" });
        db.VisitPurposes.Add(new VisitPurpose { Name = "ประชุม" });
        db.Gates.Add(new Gate { Name = "ประตูใหญ่" });
        db.BlacklistEntries.Add(new BlacklistEntry
        {
            NationalId = "1234567890121",
            FullName = "นาย ไม่พึงประสงค์ ตัวอย่าง",
            Reason = "เคยฝ่าฝืนระเบียบความปลอดภัย",
            IsActive = true,
            CreatedAt = TimeHelper.Now
        });
        await db.SaveChangesAsync();
    }

    public static CheckInViewModel ValidCheckIn(AppDbContext db) => new()
    {
        NationalId = "1103700156780",
        Title = "นาย",
        FirstName = "ทดลอง",
        LastName = "เข้าพบ",
        Phone = "081-222-3333",
        CompanyName = "บริษัท ใหม่ จำกัด",
        VisitorTypeId = db.VisitorTypes.First().Id,
        VisitPurposeId = db.VisitPurposes.First().Id,
        HostName = db.Employees.First().FullName,
        GateId = db.Gates.First().Id,
        ExpectedHours = 2,
        VehicleType = "รถยนต์",
        VehiclePlate = "กข 1234",
        PdpaConsent = true,
        SubmitAction = "checkin"
    };

    public static VisitRegistrationService CreateRegistration(AppDbContext db, IPhotoStorageService? photos = null) =>
        new(db, new VisitNumberService(db), new BlacklistService(db), photos ?? new NullPhotos(), new AuditService(db), new NullCloudSync(), new TestCompanyContext(db));

    private sealed class TestCompanyContext : ICompanyContext
    {
        private readonly AppDbContext _db;

        public TestCompanyContext(AppDbContext db) => _db = db;

        public Task<CompanyProfile> GetActiveAsync(CancellationToken cancellationToken = default) =>
            _db.CompanyProfiles.OrderByDescending(c => c.IsActive).ThenBy(c => c.Id).FirstAsync(cancellationToken);

        public async Task<IReadOnlyList<CompanyProfile>> ListAsync(CancellationToken cancellationToken = default) =>
            await _db.CompanyProfiles.OrderBy(c => c.CompanyCode).ToListAsync(cancellationToken);

        public async Task SetActiveAsync(int companyId, CancellationToken cancellationToken = default)
        {
            foreach (var c in await _db.CompanyProfiles.ToListAsync(cancellationToken))
            {
                c.IsActive = c.Id == companyId;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<CompanyProfile> CreateAsync(string companyCode, string name, CancellationToken cancellationToken = default)
        {
            var company = new CompanyProfile
            {
                CompanyCode = CompanyContext.NormalizeCode(companyCode),
                Name = name.Trim(),
                IsActive = true,
                SeedRevision = 2
            };
            _db.CompanyProfiles.Add(company);
            await _db.SaveChangesAsync(cancellationToken);
            return company;
        }
    }

    public sealed class RecordingPhotos : IPhotoStorageService
    {
        public List<string> SavedStems { get; } = [];

        public Task<string?> SaveDataUrlAsync(string? dataUrl, string fileStem, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                return Task.FromResult<string?>(null);
            }

            SavedStems.Add(fileStem);
            return Task.FromResult<string?>("/uploads/photos/" + fileStem + ".jpg");
        }

        public string? PublicUrl(string? relativePath) => relativePath;
    }

    private sealed class NullPhotos : IPhotoStorageService
    {
        public Task<string?> SaveDataUrlAsync(string? dataUrl, string fileStem, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public string? PublicUrl(string? relativePath) => relativePath;
    }

    private sealed class NullCloudSync : ICloudVisitSyncService
    {
        public Task<bool> TrySyncVisitAsync(int visitId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> SyncPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> ProbeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
