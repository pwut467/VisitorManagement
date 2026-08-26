using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class CloudOptionsTests
{
    [Fact]
    public void BuildsTrustedConnectionWhenUserMissing()
    {
        var cs = CloudOptions.BuildConnectionString(new CloudOptions
        {
            Server = "192.168.11.204",
            Database = "VisitorManagment"
        });

        Assert.Contains("Server=192.168.11.204", cs);
        Assert.Contains("Database=VisitorManagment", cs);
        Assert.Contains("Trusted_Connection=True", cs);
        Assert.DoesNotContain("User Id=", cs);
    }

    [Fact]
    public void BuildsSqlAuthWhenUserProvided()
    {
        var cs = CloudOptions.BuildConnectionString(new CloudOptions
        {
            Server = "192.168.11.204",
            Database = "VisitorManagment",
            UserId = "VisitorApp",
            Password = "secret"
        });

        Assert.Contains("User Id=VisitorApp", cs);
        Assert.Contains("Password=secret", cs);
        Assert.DoesNotContain("Trusted_Connection=True", cs);
    }
}
