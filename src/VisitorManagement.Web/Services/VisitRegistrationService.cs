using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.ViewModels;

namespace VisitorManagement.Web.Services;

public class VisitOperationResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public Visit? Visit { get; init; }
    public BlacklistEntry? Blacklist { get; init; }

    public static VisitOperationResult Fail(string error, BlacklistEntry? blacklist = null) =>
        new() { Succeeded = false, Error = error, Blacklist = blacklist };

    public static VisitOperationResult Ok(Visit visit) =>
        new() { Succeeded = true, Visit = visit };
}

public interface IVisitRegistrationService
{
    Task<VisitOperationResult> RegisterAsync(CheckInViewModel model, string userId, CancellationToken cancellationToken = default);
    Task<VisitOperationResult> CheckOutAsync(string visitCodeOrNumber, int? gateOutId, string userId, string? notes, CancellationToken cancellationToken = default);
    Task<Visitor?> FindVisitorAsync(string nationalId, CancellationToken cancellationToken = default);
}

public class VisitRegistrationService : IVisitRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IVisitNumberService _numbers;
    private readonly IBlacklistService _blacklist;
    private readonly IPhotoStorageService _photos;
    private readonly IAuditService _audit;
    private readonly ICloudVisitSyncService _cloudSync;
    private readonly ICompanyContext _companyContext;

    public VisitRegistrationService(
        AppDbContext db,
        IVisitNumberService numbers,
        IBlacklistService blacklist,
        IPhotoStorageService photos,
        IAuditService audit,
        ICloudVisitSyncService cloudSync,
        ICompanyContext companyContext)
    {
        _db = db;
        _numbers = numbers;
        _blacklist = blacklist;
        _photos = photos;
        _audit = audit;
        _cloudSync = cloudSync;
        _companyContext = companyContext;
    }

    public async Task<Visitor?> FindVisitorAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        var id = ThaiNationalId.Normalize(nationalId);
        var company = await _companyContext.GetActiveAsync(cancellationToken);
        return await _db.Visitors.FirstOrDefaultAsync(
            v => v.CompanyProfileId == company.Id && v.NationalId == id,
            cancellationToken);
    }

    public async Task<VisitOperationResult> RegisterAsync(CheckInViewModel model, string userId, CancellationToken cancellationToken = default)
    {
        var nationalId = ThaiNationalId.Normalize(model.NationalId);
        if (!ThaiNationalId.IsValid(nationalId))
        {
            return VisitOperationResult.Fail("เลขบัตรประชาชนไม่ถูกต้อง (ตรวจสอบ checksum 13 หลัก)");
        }

        if (model.SubmitAction != "preregister" && !model.PdpaConsent)
        {
            return VisitOperationResult.Fail("ต้องได้รับความยินยอม PDPA ก่อนลงทะเบียนเข้าพื้นที่");
        }

        if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
        {
            return VisitOperationResult.Fail("กรุณากรอกชื่อและนามสกุล");
        }

        if (model.VisitorTypeId is not > 0)
        {
            return VisitOperationResult.Fail("กรุณาเลือกประเภทผู้มาติดต่อ");
        }

        if (model.VisitPurposeId is not > 0)
        {
            return VisitOperationResult.Fail("กรุณาเลือกวัตถุประสงค์");
        }

        if (string.IsNullOrWhiteSpace(model.VehicleType))
        {
            return VisitOperationResult.Fail("กรุณาเลือกประเภทรถ");
        }

        if (string.IsNullOrWhiteSpace(model.VehiclePlate))
        {
            return VisitOperationResult.Fail("กรุณากรอกทะเบียนรถ");
        }

        var host = await ResolveHostEmployeeAsync(model.HostName, cancellationToken);
        if (host is null)
        {
            return VisitOperationResult.Fail("กรุณากรอกชื่อพนักงานที่มาติดต่อ");
        }

        var fullName = $"{model.Title} {model.FirstName} {model.LastName}".Trim();
        var blocked = await _blacklist.FindActiveAsync(nationalId, fullName, cancellationToken);
        if (blocked is not null)
        {
            return VisitOperationResult.Fail($"บุคคลนี้อยู่ในบัญชีดำ: {blocked.Reason}", blocked);
        }

        var company = await _companyContext.GetActiveAsync(cancellationToken);
        var now = TimeHelper.Now;
        var visitor = await FindVisitorAsync(nationalId, cancellationToken);
        var isNewVisitor = visitor is null;
        if (isNewVisitor)
        {
            visitor = new Visitor
            {
                CompanyProfileId = company.Id,
                NationalId = nationalId,
                Title = model.Title ?? "",
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Phone = model.Phone?.Trim(),
                Email = null,
                DateOfBirth = null,
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Visitors.Add(visitor);
        }

        Visit visit;
        if (model.VisitId is int existingId)
        {
            visit = await _db.Visits.FirstOrDefaultAsync(
                        v => v.Id == existingId && v.CompanyProfileId == company.Id,
                        cancellationToken)
                    ?? throw new InvalidOperationException("ไม่พบรายการลงทะเบียนล่วงหน้า");
            if (visit.Status is not VisitStatus.PreRegistered and not VisitStatus.CheckedIn)
            {
                return VisitOperationResult.Fail("รายการนี้ไม่สามารถบันทึกเข้าได้");
            }
        }
        else
        {
            visit = new Visit
            {
                CompanyProfileId = company.Id,
                HostCompanyCode = company.CompanyCode,
                VisitNumber = await _numbers.NextAsync(now, company.Id, company.CompanyCode, cancellationToken),
                VisitCode = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                RegisteredByUserId = await ResolveExistingUserIdAsync(userId, cancellationToken)
            };
            _db.Visits.Add(visit);
        }

        var gateId = model.GateId > 0
            ? model.GateId
            : await _db.Gates.Where(g => g.IsActive).Select(g => g.Id).FirstOrDefaultAsync(cancellationToken);
        var hours = model.ExpectedHours > 0 ? model.ExpectedHours : company.DefaultVisitHours;
        if (hours <= 0)
        {
            hours = 2;
        }

        visit.CompanyProfileId = company.Id;
        visit.HostCompanyCode = company.CompanyCode;
        visit.Visitor = visitor!;
        visit.VisitorTypeId = model.VisitorTypeId.Value;
        visit.VisitPurposeId = model.VisitPurposeId.Value;
        visit.HostEmployee = host;
        visit.GateInId = gateId == 0 ? null : gateId;
        visit.CompanyName = model.CompanyName?.Trim();
        // Always store the typed identity for this visit; never overwrite an existing Visitor master name.
        visit.GuestTitle = model.Title ?? "";
        visit.GuestFirstName = model.FirstName.Trim();
        visit.GuestLastName = model.LastName.Trim();
        visit.GuestPhone = model.Phone?.Trim();
        visit.PurposeDetail = model.PurposeDetail?.Trim();
        visit.VehiclePlate = model.VehiclePlate?.Trim();
        visit.VehicleType = model.VehicleType?.Trim();
        visit.ItemsBrought = null;
        visit.AccompanyingCount = model.AccompanyingCount;
        visit.AccompanyingNames = null;
        visit.RequiresEscort = false;
        visit.AccessArea = null;
        visit.Notes = model.Notes?.Trim();

        var isPreReg = string.Equals(model.SubmitAction, "preregister", StringComparison.OrdinalIgnoreCase);
        if (isPreReg)
        {
            visit.Status = VisitStatus.PreRegistered;
            visit.AppointmentAt = now;
        }
        else
        {
            visit.Status = VisitStatus.CheckedIn;
            visit.CheckInAt = now;
            visit.ExpectedCheckoutAt = now.AddHours(hours);
            visit.PdpaConsentAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var webcamPhoto = await _photos.SaveDataUrlAsync(model.PhotoDataUrl, visit.VisitCode + "-webcam", cancellationToken);
        var cardPhoto = await _photos.SaveDataUrlAsync(model.CardPhotoDataUrl, visit.VisitCode + "-card", cancellationToken);
        if (webcamPhoto is not null || cardPhoto is not null)
        {
            if (webcamPhoto is not null)
            {
                visit.PhotoPath = webcamPhoto;
            }

            if (cardPhoto is not null)
            {
                visit.CardPhotoPath = cardPhoto;
            }

            if (isNewVisitor)
            {
                if (webcamPhoto is not null)
                {
                    visitor!.PhotoPath = webcamPhoto;
                }

                if (cardPhoto is not null)
                {
                    visitor!.CardPhotoPath = cardPhoto;
                }
            }
            else if (visitor is not null && cardPhoto is not null)
            {
                // Refresh master card photo when the same NationalId returns with a new chip image.
                visitor.CardPhotoPath = cardPhoto;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.WriteAsync(
            userId,
            isPreReg ? "pre-register" : "check-in",
            nameof(Visit),
            visit.Id.ToString(),
            visit.VisitNumber,
            null);

        visit.CloudSynced = false;
        visit.CloudSyncError = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _cloudSync.TrySyncVisitAsync(visit.Id, cancellationToken);

        return VisitOperationResult.Ok(visit);
    }

    private async Task<Employee?> ResolveHostEmployeeAsync(string? hostName, CancellationToken cancellationToken)
    {
        var name = NormalizePersonName(hostName);
        if (name.Length == 0)
        {
            return null;
        }

        var employees = await _db.Employees.ToListAsync(cancellationToken);
        var match = employees.FirstOrDefault(e =>
            string.Equals(NormalizePersonName(e.FullName), name, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            match.IsActive = true;
            match.FullName = name;
            return match;
        }

        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Code == "GEN", cancellationToken)
            ?? await _db.Departments.FirstOrDefaultAsync(cancellationToken);
        if (department is null)
        {
            department = new Department { Code = "GEN", Name = "ทั่วไป" };
            _db.Departments.Add(department);
        }

        var host = new Employee
        {
            EmployeeCode = await NextTypedHostCodeAsync(cancellationToken),
            FullName = name,
            Department = department,
            IsActive = true
        };
        _db.Employees.Add(host);
        return host;
    }

    private async Task<string> NextTypedHostCodeAsync(CancellationToken cancellationToken)
    {
        var stamp = TimeHelper.Now.ToString("yyyyMMddHHmmss");
        var prefix = "H" + stamp;
        var exists = await _db.Employees.AnyAsync(e => e.EmployeeCode == prefix, cancellationToken);
        return exists ? prefix + "-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant() : prefix;
    }

    private static string NormalizePersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<string?> ResolveExistingUserIdAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken);
        return exists ? userId : null;
    }

    public async Task<VisitOperationResult> CheckOutAsync(
        string visitCodeOrNumber,
        int? gateOutId,
        string userId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var key = visitCodeOrNumber.Trim();
        if (key.StartsWith("VISIT|", StringComparison.OrdinalIgnoreCase))
        {
            key = key[6..];
        }

        var company = await _companyContext.GetActiveAsync(cancellationToken);
        var visit = await _db.Visits
            .Include(v => v.Visitor)
            .FirstOrDefaultAsync(
                v => v.CompanyProfileId == company.Id && (v.VisitCode == key || v.VisitNumber == key),
                cancellationToken);

        if (visit is null)
        {
            return VisitOperationResult.Fail("ไม่พบบัตร Visitor หรือรหัสไม่ถูกต้อง");
        }

        if (visit.Status == VisitStatus.CheckedOut)
        {
            return VisitOperationResult.Fail($"รายการ {visit.VisitNumber} ออกจากพื้นที่ไปแล้วเมื่อ {visit.CheckOutAt:dd/MM/yyyy HH:mm}");
        }

        if (visit.Status != VisitStatus.CheckedIn)
        {
            return VisitOperationResult.Fail("รายการนี้ยังไม่ได้ Check-in");
        }

        visit.Status = VisitStatus.CheckedOut;
        visit.CheckOutAt = TimeHelper.Now;
        visit.GateOutId = gateOutId;
        visit.CheckedOutByUserId = await ResolveExistingUserIdAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            visit.Notes = string.IsNullOrWhiteSpace(visit.Notes) ? notes : visit.Notes + " | " + notes;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "check-out", nameof(Visit), visit.Id.ToString(), visit.VisitNumber, null);

        visit.CloudSynced = false;
        visit.CloudSyncError = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _cloudSync.TrySyncVisitAsync(visit.Id, cancellationToken);

        return VisitOperationResult.Ok(visit);
    }
}
