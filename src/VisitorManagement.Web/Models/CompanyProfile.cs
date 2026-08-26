using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class CompanyProfile
{
    public int Id { get; set; }

    /// <summary>Short unique code used to separate visitor data across companies.</summary>
    [Required, MaxLength(20)]
    public string CompanyCode { get; set; } = "DEFAULT";

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(260)]
    public string? LogoPath { get; set; }

    [MaxLength(200)]
    public string BadgeFooter { get; set; } = "กรุณาติดบัตรนี้ตลอดเวลาที่อยู่ในบริษัท และคืนบัตรเมื่อออก";

    public int DefaultVisitHours { get; set; } = 2;

    public int OverstayGraceMinutes { get; set; } = 15;

    public bool AutoPrintBadge { get; set; } = true;

    /// <summary>Default active company for new sessions / workstations.</summary>
    public bool IsActive { get; set; } = true;

    public int SeedRevision { get; set; }

    public bool CloudEnabled { get; set; } = true;

    [MaxLength(120)]
    public string CloudServer { get; set; } = "192.168.11.204";

    [MaxLength(120)]
    public string CloudDatabase { get; set; } = "VisitorManagment";

    public bool CloudUseWindowsAuth { get; set; }

    [MaxLength(100)]
    public string? CloudUserId { get; set; }

    [MaxLength(200)]
    public string? CloudPassword { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    public ICollection<Visitor> Visitors { get; set; } = new List<Visitor>();
}
