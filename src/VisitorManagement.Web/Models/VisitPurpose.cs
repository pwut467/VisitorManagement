using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class VisitPurpose
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
