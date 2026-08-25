using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class CompanyProfile
{
    public int Id { get; set; }

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

    public int SeedRevision { get; set; }
}
