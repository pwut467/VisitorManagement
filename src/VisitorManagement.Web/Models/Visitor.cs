using System.ComponentModel.DataAnnotations;

namespace VisitorManagement.Web.Models;

public class Visitor
{
    public int Id { get; set; }

    [Required, MaxLength(13)]
    public string NationalId { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public string FullName => $"{Title} {FirstName} {LastName}".Trim();
}
