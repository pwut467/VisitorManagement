namespace VisitorManagement.CardReader.Core;

public sealed class ThaiIdCardClient
{
    private static readonly byte[] SelectApplet =
    [
        0x00, 0xA4, 0x04, 0x00, 0x08,
        0xA0, 0x00, 0x00, 0x00, 0x54, 0x48, 0x00, 0x01
    ];

    private readonly ICardTransport _transport;
    private readonly byte _getResponseP2;

    public ThaiIdCardClient(ICardTransport transport)
    {
        _transport = transport;
        var atr = transport.Atr;
        _getResponseP2 = atr.Length >= 2 && atr[0] == 0x3B && atr[1] == 0x67
            ? (byte)0x01
            : (byte)0x00;
    }

    public ThaiIdCardData Read(bool includePhoto = true)
    {
        Select();

        var cid = ThaiIdCardParser.DecodeTis620(ReadField(0x00, 0x04, 0x0D));
        cid = new string(cid.Where(char.IsDigit).ToArray());
        if (cid.Length != 13)
        {
            throw new ThaiIdCardException("invalid_card", "อ่านเลขบัตรประชาชนจากบัตรไม่สำเร็จ");
        }

        var (title, first, middle, last) = ThaiIdCardParser.ParseName(ReadField(0x00, 0x11, 0x64));
        byte[]? photo = null;
        if (includePhoto)
        {
            try
            {
                photo = ReadPhoto();
            }
            catch (ThaiIdCardException)
            {
                photo = null;
            }
        }

        return new ThaiIdCardData
        {
            NationalId = cid,
            Title = title,
            FirstName = first,
            MiddleName = middle,
            LastName = last,
            Address = ThaiIdCardParser.ParseAddress(ReadField(0x15, 0x79, 0x64)),
            DateOfBirth = ThaiIdCardParser.ParseBuddhistDate(ReadField(0x00, 0xD9, 0x08)),
            Gender = ThaiIdCardParser.ParseGender(ReadField(0x00, 0xE1, 0x01)),
            Issuer = ThaiIdCardParser.DecodeTis620(ReadField(0x00, 0xF6, 0x64)),
            IssueDate = ThaiIdCardParser.ParseBuddhistDate(ReadField(0x01, 0x67, 0x08)),
            ExpireDate = ThaiIdCardParser.ParseBuddhistDate(ReadField(0x01, 0x6F, 0x08)),
            PhotoJpeg = photo,
            ReaderName = _transport.ReaderName
        };
    }

    private void Select()
    {
        var (data, sw1, sw2) = Exchange(SelectApplet, 0x00);
        if (sw1 == 0x90 && sw2 == 0x00)
        {
            return;
        }

        throw new ThaiIdCardException("not_thai_id",
            $"บัตรนี้ไม่ใช่บัตรประชาชนไทย หรือเลือกแอปเพล็ตไม่สำเร็จ ({sw1:X2}{sw2:X2})");
    }

    private byte[] ReadField(byte p1, byte p2, byte length)
    {
        var command = new byte[] { 0x80, 0xB0, p1, p2, 0x02, 0x00, length };
        var (data, sw1, sw2) = Exchange(command, length);
        if (sw1 != 0x90 || sw2 != 0x00)
        {
            throw new ThaiIdCardException("read_failed",
                $"อ่านข้อมูลจากบัตรไม่สำเร็จ ({sw1:X2}{sw2:X2})");
        }

        return data;
    }

    private byte[] ReadPhoto()
    {
        using var buffer = new MemoryStream();
        for (byte i = 1; i <= 20; i++)
        {
            var p2 = (byte)(0x7C - i);
            var chunk = ReadField(i, p2, 0xFF);
            buffer.Write(chunk, 0, chunk.Length);
        }

        return ThaiIdCardParser.ExtractJpeg(buffer.ToArray());
    }

    private (byte[] Data, byte Sw1, byte Sw2) Exchange(byte[] command, byte fallbackLength)
    {
        var (data, sw1, sw2) = _transport.Transmit(command);
        if (sw1 == 0x61)
        {
            return _transport.Transmit(GetResponse(sw2));
        }

        if (data.Length == 0 && sw1 == 0x90 && sw2 == 0x00 && fallbackLength > 0)
        {
            var more = _transport.Transmit(GetResponse(fallbackLength));
            if (more.Data.Length > 0)
            {
                return more;
            }
        }

        return (data, sw1, sw2);
    }

    private byte[] GetResponse(byte length) => [0x00, 0xC0, 0x00, _getResponseP2, length];
}
