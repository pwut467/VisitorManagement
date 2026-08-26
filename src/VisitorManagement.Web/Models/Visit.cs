using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class Visit
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string VisitNumber { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string VisitCode { get; set; } = string.Empty;

    public int VisitorId { get; set; }
    public Visitor Visitor { get; set; } = null!;

    public int VisitorTypeId { get; set; }
    public VisitorType VisitorType { get; set; } = null!;

    public int VisitPurposeId { get; set; }
    public VisitPurpose VisitPurpose { get; set; } = null!;

    public int HostEmployeeId { get; set; }
    public Employee HostEmployee { get; set; } = null!;

    public int? GateInId { get; set; }
    public Gate? GateIn { get; set; }

    public int? GateOutId { get; set; }
    public Gate? GateOut { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>Name typed for this visit (kept even if it differs from the Visitor master linked by NationalId).</summary>
    [MaxLength(20)]
    public string GuestTitle { get; set; } = string.Empty;

    [MaxLength(80)]
    public string GuestFirstName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string GuestLastName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? GuestPhone { get; set; }

    [MaxLength(300)]
    public string? PurposeDetail { get; set; }

    [MaxLength(20)]
    public string? VehiclePlate { get; set; }

    [MaxLength(40)]
    public string? VehicleType { get; set; }

    [MaxLength(500)]
    public string? ItemsBrought { get; set; }

    public int AccompanyingCount { get; set; }

    [MaxLength(300)]
    public string? AccompanyingNames { get; set; }

    public bool RequiresEscort { get; set; }

    [MaxLength(120)]
    public string? AccessArea { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime? AppointmentAt { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public DateTime? ExpectedCheckoutAt { get; set; }
    public DateTime? BadgePrintedAt { get; set; }
    public DateTime? PdpaConsentAt { get; set; }

    public VisitStatus Status { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }

    public string? RegisteredByUserId { get; set; }
    public ApplicationUser? RegisteredByUser { get; set; }

    public string? CheckedOutByUserId { get; set; }
    public ApplicationUser? CheckedOutByUser { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>True when this visit row has been successfully pushed to the cloud database.</summary>
    public bool CloudSynced { get; set; }

    public DateTime? CloudSyncedAt { get; set; }

    [MaxLength(400)]
    public string? CloudSyncError { get; set; }

    public ICollection<VisitItem> Items { get; set; } = new List<VisitItem>();

    public string GuestFullName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(GuestFirstName) || !string.IsNullOrWhiteSpace(GuestLastName))
            {
                return $"{GuestTitle} {GuestFirstName} {GuestLastName}".Trim();
            }

            return Visitor?.FullName ?? string.Empty;
        }
    }

    public bool HasNameMismatchWithMaster
    {
        get
        {
            if (Visitor is null || string.IsNullOrWhiteSpace(GuestFirstName))
            {
                return false;
            }

            return !string.Equals(GuestFirstName.Trim(), Visitor.FirstName.Trim(), StringComparison.OrdinalIgnoreCase)
                   || !string.Equals(GuestLastName.Trim(), Visitor.LastName.Trim(), StringComparison.OrdinalIgnoreCase)
                   || !string.Equals((GuestTitle ?? "").Trim(), (Visitor.Title ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsOnSite => Status == VisitStatus.CheckedIn;

    public bool IsOverstay(DateTime now) =>
        Status == VisitStatus.CheckedIn &&
        ExpectedCheckoutAt.HasValue &&
        ExpectedCheckoutAt.Value < now;
}
