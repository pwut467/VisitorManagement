using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.ViewModels;

public class SettingsViewModel
{
    [Required, Display(Name = "ชื่อบริษัท")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "ที่อยู่")]
    public string? Address { get; set; }

    [Display(Name = "ข้อความท้ายบัตร Visitor")]
    public string BadgeFooter { get; set; } = string.Empty;

    [Range(1, 24), Display(Name = "ชั่วโมงเข้าพบเริ่มต้น")]
    public int DefaultVisitHours { get; set; }

    [Range(0, 240), Display(Name = "ผ่อนผันเกินเวลา (นาที)")]
    public int OverstayGraceMinutes { get; set; }

    [Display(Name = "หลังลงทะเบียน Check-in")]
    public bool AutoPrintBadge { get; set; } = true;
}
