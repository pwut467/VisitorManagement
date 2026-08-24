using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VisitorManagement.Web.ViewModels;

public class CheckInViewModel
{
    public int? VisitId { get; set; }

    [Display(Name = "เลขบัตรประชาชน")]
    [Required(ErrorMessage = "กรุณากรอกเลขบัตรประชาชน")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "เลขบัตรประชาชนต้องมี 13 หลัก")]
    public string NationalId { get; set; } = string.Empty;

    [Display(Name = "คำนำหน้า")]
    public string Title { get; set; } = "นาย";

    [Display(Name = "ชื่อ")]
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "นามสกุล")]
    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "เบอร์โทร")]
    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "อีเมล")]
    public string? Email { get; set; }

    [Display(Name = "บริษัท / หน่วยงาน")]
    public string? CompanyName { get; set; }

    [Display(Name = "ที่อยู่")]
    public string? Address { get; set; }

    [Display(Name = "ประเภทผู้มาติดต่อ")]
    [Required]
    public int VisitorTypeId { get; set; }

    [Display(Name = "วัตถุประสงค์")]
    [Required]
    public int VisitPurposeId { get; set; }

    [Display(Name = "รายละเอียดเรื่องที่มา")]
    public string? PurposeDetail { get; set; }

    [Display(Name = "มาติดต่อ (พนักงาน)")]
    [Required]
    public int HostEmployeeId { get; set; }

    [Display(Name = "จุดเข้า")]
    [Required]
    public int GateId { get; set; }

    [Display(Name = "ระยะเวลาที่คาดว่าจะอยู่ (ชั่วโมง)")]
    [Range(1, 24)]
    public int ExpectedHours { get; set; } = 2;

    [Display(Name = "ทะเบียนรถ")]
    public string? VehiclePlate { get; set; }

    [Display(Name = "ประเภทรถ")]
    public string? VehicleType { get; set; }

    [Display(Name = "สิ่งของที่นำเข้า")]
    public string? ItemsBrought { get; set; }

    [Display(Name = "จำนวนผู้ติดตาม")]
    [Range(0, 50)]
    public int AccompanyingCount { get; set; }

    [Display(Name = "ชื่อผู้ติดตาม")]
    public string? AccompanyingNames { get; set; }

    [Display(Name = "ต้องมีพนักงาน escort")]
    public bool RequiresEscort { get; set; }

    [Display(Name = "พื้นที่ที่อนุญาต")]
    public string? AccessArea { get; set; }

    [Display(Name = "หมายเหตุ")]
    public string? Notes { get; set; }

    [Display(Name = "ยินยอมให้เก็บข้อมูลตาม PDPA")]
    public bool PdpaConsent { get; set; }

    public string? PhotoDataUrl { get; set; }

    public string SubmitAction { get; set; } = "checkin";

    public IEnumerable<SelectListItem> Titles { get; set; } = [];
    public IEnumerable<SelectListItem> VisitorTypes { get; set; } = [];
    public IEnumerable<SelectListItem> VisitPurposes { get; set; } = [];
    public IEnumerable<SelectListItem> Hosts { get; set; } = [];
    public IEnumerable<SelectListItem> Gates { get; set; } = [];
    public IEnumerable<SelectListItem> VehicleTypes { get; set; } = [];
}
