using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class VisitorType
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string BadgeLabel { get; set; } = "VISITOR";

    [MaxLength(20)]
    public string Color { get; set; } = "#1a56a0";

    public bool RequiresEscortDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
