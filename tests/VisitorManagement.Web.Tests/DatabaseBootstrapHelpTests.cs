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
        Assert.Contains("TCP/IP", hint);
        Assert.Contains("localhost", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildHelpMessage_IncludesPipeSpecificGuidance()
    {
        var message = DatabaseBootstrap.BuildHelpMessage(
            "Server=.\\SQLEXPRESS; Database=VisitorManagment; Trusted_Connection",
            new InvalidOperationException("No process is on the other end of the pipe."));
        Assert.Contains("No process is on the other end of the pipe", message);
        Assert.Contains("TCP/IP", message);
        Assert.Contains("VisitorManagment", message);
    }

    [Fact]
    public void BuildConnectionCandidates_AddsTcpAlternativesForExpress()
    {
        var candidates = DatabaseBootstrap.BuildConnectionCandidates(
            @"Server=.\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True");
        Assert.Contains(candidates, cs => cs.Contains("tcp:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(candidates, cs => cs.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(candidates, cs => cs.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }
}
