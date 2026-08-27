using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool IsActive { get; set; } = true;
}
