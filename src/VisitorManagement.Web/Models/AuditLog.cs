using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class AuditLog
{
    public long Id { get; set; }

    [MaxLength(80)]
    public string? ActorUserId { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EntityType { get; set; }

    [MaxLength(40)]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Detail { get; set; }

    [MaxLength(60)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
