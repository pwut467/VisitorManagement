using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.ViewModels;

public class EmployeeFormViewModel
{
    public int? Id { get; set; }

    [Required, Display(Name = "รหัสพนักงาน")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required, Display(Name = "แผนก")]
    public int DepartmentId { get; set; }

    [Display(Name = "เบอร์โทร")]
    public string? Phone { get; set; }

    [Display(Name = "อีเมล")]
    public string? Email { get; set; }

    [Display(Name = "ใช้งาน")]
    public bool IsActive { get; set; } = true;
}
