using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.ViewModels;

public class VisitListFilter
{
    public string? Q { get; set; }
    public VisitStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? HostEmployeeId { get; set; }
    public int? VisitorTypeId { get; set; }
    public bool OnSiteOnly { get; set; }
}
