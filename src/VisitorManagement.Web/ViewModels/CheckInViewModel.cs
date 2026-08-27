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
    public string? Phone { get; set; }

    [Display(Name = "บริษัท / หน่วยงาน")]
    public string? CompanyName { get; set; }

    public string? Address { get; set; }

    [Display(Name = "ประเภทผู้มาติดต่อ")]
    [Required(ErrorMessage = "กรุณาเลือกประเภทผู้มาติดต่อ")]
    public int? VisitorTypeId { get; set; }

    [Display(Name = "วัตถุประสงค์")]
    [Required(ErrorMessage = "กรุณาเลือกวัตถุประสงค์")]
    public int? VisitPurposeId { get; set; }

    [Display(Name = "รายละเอียดเรื่องที่มา")]
    public string? PurposeDetail { get; set; }

    [Display(Name = "มาติดต่อ (พนักงาน)")]
    [Required(ErrorMessage = "กรุณากรอกชื่อพนักงานที่มาติดต่อ")]
    [MaxLength(150)]
    public string HostName { get; set; } = string.Empty;

    public int GateId { get; set; }

    public int ExpectedHours { get; set; } = 2;

    [Display(Name = "ทะเบียนรถ")]
    [Required(ErrorMessage = "กรุณากรอกทะเบียนรถ")]
    [MaxLength(20)]
    public string VehiclePlate { get; set; } = string.Empty;

    [Display(Name = "ประเภทรถ")]
    [Required(ErrorMessage = "กรุณาเลือกประเภทรถ")]
    [MaxLength(40)]
    public string VehicleType { get; set; } = string.Empty;

    [Display(Name = "จำนวนผู้ติดตาม")]
    [Range(0, 50)]
    public int AccompanyingCount { get; set; }

    [Display(Name = "หมายเหตุ")]
    public string? Notes { get; set; }

    [Display(Name = "ยินยอมให้เก็บข้อมูลตาม PDPA")]
    public bool PdpaConsent { get; set; } = true;

    public string? PhotoDataUrl { get; set; }

    public string SubmitAction { get; set; } = "checkin";

    public IEnumerable<SelectListItem> Titles { get; set; } = [];
    public IEnumerable<SelectListItem> VisitorTypes { get; set; } = [];
    public IEnumerable<SelectListItem> VisitPurposes { get; set; } = [];
    public IEnumerable<SelectListItem> VehicleTypes { get; set; } = [];
}
