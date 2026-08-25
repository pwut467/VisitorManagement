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
    }
}

public class VisitNumberServiceTests
{
    [Fact]
    public async Task SequencesPerDay()
    {
        var db = TestDb.Create();
        var svc = new VisitNumberService(db);
        var day = new DateTime(2026, 8, 24);
        var first = await svc.NextAsync(day);
        db.Visits.Add(MinimalVisit(db, first));
        await db.SaveChangesAsync();
        var second = await svc.NextAsync(day);
        Assert.Equal("V20260824-0001", first);
        Assert.Equal("V20260824-0002", second);
    }

    private static Visit MinimalVisit(AppDbContext db, string number)
    {
        var dept = new Department { Code = "X", Name = "X" };
        var emp = new Employee { EmployeeCode = "Z1", FullName = "Host", Department = dept };
        var type = new VisitorType { Name = "Guest", BadgeLabel = "GUEST" };
        var purpose = new VisitPurpose { Name = "Meet" };
        var visitor = new Visitor { NationalId = "3101700123452", FirstName = "A", LastName = "B", CreatedAt = TimeHelper.Now, UpdatedAt = TimeHelper.Now };
        return new Visit
        {
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
        Assert.StartsWith("V", result.Visit.VisitNumber);

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
    public async Task ReturningVisitorIsReused()
    {
        var db = TestDb.Create();
        await TestDb.SeedGraphAsync(db);
        var svc = TestDb.CreateRegistration(db);
        await svc.RegisterAsync(TestDb.ValidCheckIn(db), "u1");
        var again = TestDb.ValidCheckIn(db);
        again.Phone = "089-000-1111";
        again.FirstName = "ทดลอง";
        var second = await svc.RegisterAsync(again, "u1");
        Assert.True(second.Succeeded, second.Error);
        Assert.Equal(1, await db.Visitors.CountAsync());
        Assert.Equal(2, await db.Visits.CountAsync());
        Assert.Equal("089-000-1111", db.Visitors.Single().Phone);
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

    public static VisitRegistrationService CreateRegistration(AppDbContext db) =>
        new(db, new VisitNumberService(db), new BlacklistService(db), new NullPhotos(), new AuditService(db));

    private sealed class NullPhotos : IPhotoStorageService
    {
        public Task<string?> SaveDataUrlAsync(string? dataUrl, string fileStem, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public string? PublicUrl(string? relativePath) => relativePath;
    }
}
