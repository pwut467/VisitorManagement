using VisitorManagement.Web;

namespace VisitorManagement.Web.Tests;

public class ViewFlagTests
{
    [Fact]
    public void BoxedTrueIsOn()
    {
        object boxed = true;
        Assert.True(ViewFlag.IsOn(boxed));
        Assert.False(ViewFlag.IsOn((object)false));
        Assert.True(ViewFlag.IsOn("true"));
        Assert.False(ViewFlag.IsOn("false"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(null, false)]
    public void IsOnReadsStoredFlags(object? value, bool expected)
    {
        Assert.Equal(expected, ViewFlag.IsOn(value));
    }

    [Fact]
    public void MissingFlagUsesFallback()
    {
        Assert.True(ViewFlag.IsOn(null, whenMissing: true));
        Assert.False(ViewFlag.IsOn(false, whenMissing: true));
        Assert.True(ViewFlag.IsOn(true, whenMissing: false));
    }
}
