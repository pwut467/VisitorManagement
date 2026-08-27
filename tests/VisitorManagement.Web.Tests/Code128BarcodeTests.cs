using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class Code128BarcodeTests
{
    [Fact]
    public void EncodesStartChecksumAndStop()
    {
        var codes = Code128Barcode.Encode("A");
        Assert.Equal(104, codes[0]);
        Assert.Equal(33, codes[1]);
        Assert.Equal((104 + 33) % 103, codes[2]);
        Assert.Equal(106, codes[3]);
    }

    [Fact]
    public void SvgContainsVisitNumberAndBars()
    {
        var svg = Code128Barcode.Svg("V20260825-0001");
        Assert.Contains("V20260825-0001", svg);
        Assert.Contains("<rect", svg);
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("<script", svg);
    }

    [Fact]
    public void RejectsNonAscii()
    {
        Assert.Throws<ArgumentException>(() => Code128Barcode.Encode("เวิน"));
    }
}
