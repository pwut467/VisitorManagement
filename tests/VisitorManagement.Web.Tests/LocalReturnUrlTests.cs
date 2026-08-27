namespace VisitorManagement.Web.Tests;

public class LocalReturnUrlTests
{
    private static bool IsLocal(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//");

    [Fact]
    public void PrefersExplicitReturnUrl()
    {
        var result = LocalReturnUrl.Resolve("/Dashboard", "http://localhost/Visits", IsLocal, "/Visits");
        Assert.Equal("/Dashboard", result);
    }

    [Fact]
    public void KeepsQueryStringOnExplicitReturnUrl()
    {
        var result = LocalReturnUrl.Resolve("/Visits?Q=abc&Status=CheckedIn", null, IsLocal, "/Visits");
        Assert.Equal("/Visits?Q=abc&Status=CheckedIn", result);
    }

    [Fact]
    public void FallsBackToRefererPathAndQuery()
    {
        var result = LocalReturnUrl.Resolve(null, "http://localhost/Visits?Q=abc", IsLocal, "/Visits");
        Assert.Equal("/Visits?Q=abc", result);
    }

    [Fact]
    public void IgnoresDetailsAndBadgeReferers()
    {
        Assert.Equal("/Visits", LocalReturnUrl.Resolve(null, "http://localhost/Visits/Details/5", IsLocal, "/Visits"));
        Assert.Equal("/Visits", LocalReturnUrl.Resolve(null, "http://localhost/Visits/Badge/5", IsLocal, "/Visits"));
        Assert.Equal("/Visits", LocalReturnUrl.Resolve(null, "http://localhost/Account/Login", IsLocal, "/Visits"));
    }

    [Fact]
    public void RejectsExternalReturnUrlAndUsesReferer()
    {
        var result = LocalReturnUrl.Resolve("https://evil.example/phish", "http://localhost/Dashboard", IsLocal, "/Visits");
        Assert.Equal("/Dashboard", result);
    }

    [Fact]
    public void RejectsProtocolRelativeUrl()
    {
        var result = LocalReturnUrl.Resolve("//evil.example/phish", null, IsLocal, "/Visits");
        Assert.Equal("/Visits", result);
    }

    [Theory]
    [InlineData("/Visits/Details")]
    [InlineData("/Visits/Details/12")]
    [InlineData("/Visits/Badge/3")]
    [InlineData("/Visits/Qr")]
    [InlineData("/Account/Login")]
    public void NestedVisitPagesAreNotBackTargets(string path)
    {
        Assert.True(LocalReturnUrl.IsSelfOrNestedVisitPage(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Dashboard")]
    [InlineData("/Visits")]
    [InlineData("/Visits/OnSite")]
    [InlineData("/CheckOut")]
    [InlineData("/CheckIn")]
    public void ListPagesAreValidBackTargets(string path)
    {
        Assert.False(LocalReturnUrl.IsSelfOrNestedVisitPage(path));
    }
}
