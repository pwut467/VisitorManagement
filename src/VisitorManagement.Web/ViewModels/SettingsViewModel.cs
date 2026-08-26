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

    [Display(Name = "เปิดซิงก์ไป Cloud SQL")]
    public bool CloudEnabled { get; set; } = true;

    [Display(Name = "Cloud Server IP")]
    [MaxLength(120)]
    public string CloudServer { get; set; } = "192.168.11.204";

    [Display(Name = "Cloud Database")]
    [MaxLength(120)]
    public string CloudDatabase { get; set; } = "VisitorManagment";

    [Display(Name = "ใช้ Windows Authentication")]
    public bool CloudUseWindowsAuth { get; set; }

    [Display(Name = "Cloud Username (SQL Auth)")]
    [MaxLength(100)]
    public string? CloudUserId { get; set; }

    [Display(Name = "Cloud Password")]
    [MaxLength(200)]
    [DataType(DataType.Password)]
    public string? CloudPassword { get; set; }

    public bool CloudPasswordSet { get; set; }

    public string? CloudStatusLabel { get; set; }
    public string? CloudStatusDetail { get; set; }
    public bool CloudOnline { get; set; }
}
