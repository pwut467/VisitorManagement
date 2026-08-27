using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.ViewModels;

public class BlacklistFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "เลขบัตรประชาชน")]
    public string? NationalId { get; set; }

    [Required, Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required, Display(Name = "เหตุผล")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "หมดอายุ")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "ใช้งาน")]
    public bool IsActive { get; set; } = true;
}
