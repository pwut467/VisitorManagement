using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class BlacklistEntry
{
    public int Id { get; set; }

    [MaxLength(13)]
    public string? NationalId { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}
