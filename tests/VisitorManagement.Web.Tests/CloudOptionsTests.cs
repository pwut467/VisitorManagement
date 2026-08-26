using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class CloudOptionsTests
{
    [Fact]
    public void BuildConnectionString_RequiresUserId_WhenNotWindowsAuth()
    {
        var opts = new CloudOptions
        {
            Enabled = true,
            Server = "192.168.11.204",
            Database = "VisitorManagment",
            UseWindowsAuth = false,
            UserId = "",
            Password = ""
        };

        Assert.Equal("", CloudOptions.BuildConnectionString(opts));
        CloudOptions.ApplyConnectionString(opts, "Server=192.168.11.204;Database=VisitorManagment;Trusted_Connection=True");
        Assert.Null(opts.ConnectionString);
        Assert.False(opts.IsConfigured);
    }

    [Fact]
    public void BuildConnectionString_BuildsSqlAuth()
    {
        var opts = new CloudOptions
        {
            Enabled = true,
            Server = "192.168.11.204",
            Database = "VisitorManagment",
            UseWindowsAuth = false,
            UserId = "VisitorApp",
            Password = "secret",
            ConnectTimeoutSeconds = 5
        };

        var cs = CloudOptions.BuildConnectionString(opts);
        Assert.Contains("Server=192.168.11.204", cs);
        Assert.Contains("Database=VisitorManagment", cs);
        Assert.Contains("User Id=VisitorApp", cs);
        Assert.Contains("Password=secret", cs);
        Assert.Contains("TrustServerCertificate=True", cs);
        Assert.DoesNotContain("Trusted_Connection", cs);

        CloudOptions.ApplyConnectionString(opts, null);
        Assert.True(opts.IsConfigured);
        Assert.Equal(cs, opts.ConnectionString);
    }

    [Fact]
    public void BuildConnectionString_BuildsWindowsAuth_WhenRequested()
    {
        var opts = new CloudOptions
        {
            Enabled = true,
            Server = "192.168.11.204",
            Database = "VisitorManagment",
            UseWindowsAuth = true
        };

        var cs = CloudOptions.BuildConnectionString(opts);
        Assert.Contains("Trusted_Connection=True", cs);
        Assert.DoesNotContain("User Id=", cs);
        CloudOptions.ApplyConnectionString(opts, null);
        Assert.True(opts.IsConfigured);
    }

    [Fact]
    public void ApplyConnectionString_AcceptsExplicitSqlAuthConnectionString()
    {
        var opts = new CloudOptions
        {
            Enabled = true,
            Server = "192.168.11.204",
            Database = "VisitorManagment",
            UseWindowsAuth = false
        };

        CloudOptions.ApplyConnectionString(
            opts,
            "Server=192.168.11.204;Database=VisitorManagment;User Id=sa;Password=x;TrustServerCertificate=True");

        Assert.True(opts.IsConfigured);
        Assert.Contains("User Id=sa", opts.ConnectionString);
    }

    [Fact]
    public void DescribeError_MapsNetworkHints()
    {
        var mapped = CloudOptions.DescribeError(
            new InvalidOperationException("A network-related or instance-specific error occurred"));
        Assert.Contains("เข้าถึงเซิร์ฟเวอร์ Cloud ไม่ได้", mapped);
    }
}
