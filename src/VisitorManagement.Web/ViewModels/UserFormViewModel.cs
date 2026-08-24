using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.ViewModels;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required, Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "รหัสผ่าน")]
    public string? Password { get; set; }

    [Required, Display(Name = "สิทธิ์")]
    public string Role { get; set; } = "Security";

    [Display(Name = "ใช้งาน")]
    public bool IsActive { get; set; } = true;
}
