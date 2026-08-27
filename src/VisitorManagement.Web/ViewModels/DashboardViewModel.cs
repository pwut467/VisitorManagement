using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.ViewModels;

public class DashboardViewModel
{
    public int OnSiteCount { get; set; }
    public int TodayCheckIns { get; set; }
    public int TodayCheckOuts { get; set; }
    public List<Visit> OnSite { get; set; } = [];
    public List<HourlyPoint> Hourly { get; set; } = [];
}

public class HourlyPoint
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
