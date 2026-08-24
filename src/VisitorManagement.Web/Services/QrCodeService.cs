using QRCoder;

namespace VisitorManagement.Web.Services;

public interface IQrCodeService
{
    byte[] Png(string payload, int pixelsPerModule = 8);
    string DataUrl(string payload, int pixelsPerModule = 8);
}

public class QrCodeService : IQrCodeService
{
    public byte[] Png(string payload, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }

    public string DataUrl(string payload, int pixelsPerModule = 8)
    {
        var bytes = Png(payload, pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
