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

    public VisitRegistrationService(
        AppDbContext db,
        IVisitNumberService numbers,
        IBlacklistService blacklist,
        IPhotoStorageService photos,
        IAuditService audit)
    {
        _db = db;
        _numbers = numbers;
        _blacklist = blacklist;
        _photos = photos;
        _audit = audit;
    }

    public Task<Visitor?> FindVisitorAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        var id = ThaiNationalId.Normalize(nationalId);
        return _db.Visitors.FirstOrDefaultAsync(v => v.NationalId == id, cancellationToken);
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

        var fullName = $"{model.Title} {model.FirstName} {model.LastName}".Trim();
        var blocked = await _blacklist.FindActiveAsync(nationalId, fullName, cancellationToken);
        if (blocked is not null)
        {
            return VisitOperationResult.Fail($"บุคคลนี้อยู่ในบัญชีดำ: {blocked.Reason}", blocked);
        }

        var now = TimeHelper.Now;
        var visitor = await FindVisitorAsync(nationalId, cancellationToken);
        if (visitor is null)
        {
            visitor = new Visitor
            {
                NationalId = nationalId,
                CreatedAt = now
            };
            _db.Visitors.Add(visitor);
        }

        visitor.Title = model.Title;
        visitor.FirstName = model.FirstName.Trim();
        visitor.LastName = model.LastName.Trim();
        visitor.Phone = model.Phone?.Trim();
        visitor.Email = model.Email?.Trim();
        visitor.CompanyName = model.CompanyName?.Trim();
        visitor.Address = model.Address?.Trim();
        visitor.UpdatedAt = now;

        Visit visit;
        if (model.VisitId is int existingId)
        {
            visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == existingId, cancellationToken)
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
                VisitNumber = await _numbers.NextAsync(now, cancellationToken),
                VisitCode = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                RegisteredByUserId = userId
            };
            _db.Visits.Add(visit);
        }

        visit.Visitor = visitor;
        visit.VisitorTypeId = model.VisitorTypeId;
        visit.VisitPurposeId = model.VisitPurposeId;
        visit.HostEmployeeId = model.HostEmployeeId;
        visit.GateInId = model.GateId;
        visit.CompanyName = model.CompanyName?.Trim();
        visit.PurposeDetail = model.PurposeDetail?.Trim();
        visit.VehiclePlate = model.VehiclePlate?.Trim();
        visit.VehicleType = model.VehicleType?.Trim();
        visit.ItemsBrought = model.ItemsBrought?.Trim();
        visit.AccompanyingCount = model.AccompanyingCount;
        visit.AccompanyingNames = model.AccompanyingNames?.Trim();
        visit.RequiresEscort = model.RequiresEscort;
        visit.AccessArea = model.AccessArea?.Trim();
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
            visit.ExpectedCheckoutAt = now.AddHours(model.ExpectedHours);
            visit.PdpaConsentAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var photo = await _photos.SaveDataUrlAsync(model.PhotoDataUrl, visit.VisitCode, cancellationToken);
        if (photo is not null)
        {
            visit.PhotoPath = photo;
            visitor.PhotoPath = photo;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.WriteAsync(
            userId,
            isPreReg ? "pre-register" : "check-in",
            nameof(Visit),
            visit.Id.ToString(),
            visit.VisitNumber,
            null);

        return VisitOperationResult.Ok(visit);
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

        var visit = await _db.Visits
            .Include(v => v.Visitor)
            .FirstOrDefaultAsync(v => v.VisitCode == key || v.VisitNumber == key, cancellationToken);

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
        visit.CheckedOutByUserId = userId;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            visit.Notes = string.IsNullOrWhiteSpace(visit.Notes) ? notes : visit.Notes + " | " + notes;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "check-out", nameof(Visit), visit.Id.ToString(), visit.VisitNumber, null);
        return VisitOperationResult.Ok(visit);
    }
}
