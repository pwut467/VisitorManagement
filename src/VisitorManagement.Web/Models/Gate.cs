using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class Gate
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}
