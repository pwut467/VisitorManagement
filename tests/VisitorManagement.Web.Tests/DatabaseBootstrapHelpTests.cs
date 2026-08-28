using VisitorManagement.Web.Data;

namespace VisitorManagement.Web.Tests;

public class DatabaseBootstrapHelpTests
{
    [Fact]
    public void DescribeCommonFailure_ExplainsPipeError()
    {
        var hint = DatabaseBootstrap.DescribeCommonFailure("No process is on the other end of the pipe.");
        Assert.NotNull(hint);
        Assert.Contains("SQLEXPRESS", hint);
        Assert.Contains("localdb", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildHelpMessage_IncludesPipeSpecificGuidance()
    {
        var message = DatabaseBootstrap.BuildHelpMessage(
            "Server=.\\SQLEXPRESS; Database=VisitorManagment; Trusted_Connection",
            new InvalidOperationException("No process is on the other end of the pipe."));
        Assert.Contains("No process is on the other end of the pipe", message);
        Assert.Contains("services.msc", message);
        Assert.Contains("MSSQLLocalDB", message);
    }
}
