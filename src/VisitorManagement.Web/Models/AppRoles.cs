namespace VisitorManagement.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Security = "Security";
    public const string Reception = "Reception";
    public const string Host = "Host";

    public const string FrontDesk = Admin + "," + Security + "," + Reception;
    public const string Staff = FrontDesk + "," + Host;

    public static readonly string[] All = [Admin, Security, Reception, Host];
}
