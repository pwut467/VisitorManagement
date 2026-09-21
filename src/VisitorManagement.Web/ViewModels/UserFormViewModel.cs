using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.ViewModels;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล"), Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้"), Display(Name = "ชื่อผู้ใช้")]
    [StringLength(64, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z0-9._@+-]+$", ErrorMessage = "ชื่อผู้ใช้ใช้ได้เฉพาะตัวอักษร ตัวเลข และ . _ @ + -")]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง"), Display(Name = "อีเมล (ไม่บังคับ)")]
    public string? Email { get; set; }

    [DataType(DataType.Password), Display(Name = "รหัสผ่าน")]
    public string? Password { get; set; }

    [DataType(DataType.Password), Display(Name = "ยืนยันรหัสผ่าน")]
    [Compare(nameof(Password), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    public string? ConfirmPassword { get; set; }

    [Required, Display(Name = "สิทธิ์")]
    public string Role { get; set; } = "Security";

    [Display(Name = "ใช้งาน")]
    public bool IsActive { get; set; } = true;

    public bool IsOfficialAccount { get; set; }

    public bool RoleLocked { get; set; }
}
