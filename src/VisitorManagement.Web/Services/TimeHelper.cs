namespace VisitorManagement.Web.Services;

public static class TimeHelper
{
    public static TimeZoneInfo BangkokTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Bangkok");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BangkokTimeZone);

    public static DateTime Today => Now.Date;
}
