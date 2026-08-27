using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class VisitItem
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? SerialNumber { get; set; }

    public int Quantity { get; set; } = 1;
}
