using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Tests;

public class QrCodeServiceTests
{
    [Fact]
    public void DataUrl_UsesVisitPayloadPrefix()
    {
        var qr = new QrCodeService();
        var visitCode = "abc123def4567890abc123def4567890";
        var dataUrl = qr.DataUrl("VISIT|" + visitCode, pixelsPerModule: 4);

        Assert.StartsWith("data:image/png;base64,", dataUrl);
        var png = Convert.FromBase64String(dataUrl["data:image/png;base64,".Length..]);
        Assert.True(png.Length > 100);
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
    }

    [Fact]
    public void Png_IsScannableSizeForBadge()
    {
        var qr = new QrCodeService();
        var png = qr.Png("VISIT|" + Guid.NewGuid().ToString("N"), pixelsPerModule: 6);
        Assert.True(png.Length > 200);
    }
}
