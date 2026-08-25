using System.Text;
using VisitorManagement.CardReader.Core;

namespace VisitorManagement.Web.Tests;

public class ThaiIdCardParserTests
{
    public ThaiIdCardParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void ParsesThaiNameFields()
    {
        var bytes = ThaiIdCardParser.ThaiEncoding.GetBytes("นาย#สมชาย##ใจดี");
        var (title, first, middle, last) = ThaiIdCardParser.ParseName(bytes);
        Assert.Equal("นาย", title);
        Assert.Equal("สมชาย", first);
        Assert.Equal("", middle);
        Assert.Equal("ใจดี", last);
    }

    [Fact]
    public void ParsesAddressWithPrefixes()
    {
        var bytes = ThaiIdCardParser.ThaiEncoding.GetBytes("99/1#5#สุขสบาย#พหลโยธิน#ลาดยาว#จตุจักร#กรุงเทพมหานคร");
        var address = ThaiIdCardParser.ParseAddress(bytes);
        Assert.Contains("99/1", address);
        Assert.Contains("หมู่ 5", address);
        Assert.Contains("ซอย สุขสบาย", address);
        Assert.Contains("ถนน พหลโยธิน", address);
        Assert.Contains("กรุงเทพมหานคร", address);
    }

    [Fact]
    public void ConvertsBuddhistDate()
    {
        var bytes = Encoding.ASCII.GetBytes("25250512");
        Assert.Equal("1982-05-12", ThaiIdCardParser.ParseBuddhistDate(bytes));
    }

    [Fact]
    public void ParsesGender()
    {
        Assert.Equal("M", ThaiIdCardParser.ParseGender("1"u8.ToArray()));
        Assert.Equal("F", ThaiIdCardParser.ParseGender("2"u8.ToArray()));
    }

    [Fact]
    public void ExtractsJpegFromPaddedBuffer()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9, 0x00, 0x00 };
        var extracted = ThaiIdCardParser.ExtractJpeg(jpeg);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9 }, extracted);
    }
}

public class ThaiIdCardClientTests
{
    [Fact]
    public void ReadsCardThroughApduTransport()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var transport = new FakeTransport
        {
            Fields =
            {
                ["0004"] = Encoding.ASCII.GetBytes("1103700156780"),
                ["0011"] = ThaiIdCardParser.ThaiEncoding.GetBytes("นางสาว#สุดา##ทดสอบ"),
                ["1579"] = ThaiIdCardParser.ThaiEncoding.GetBytes("10## ## #แขวงคลองเตย#เขตคลองเตย#กรุงเทพมหานคร"),
                ["00D9"] = Encoding.ASCII.GetBytes("25300115"),
                ["00E1"] = "2"u8.ToArray(),
                ["00F6"] = ThaiIdCardParser.ThaiEncoding.GetBytes("กรมการปกครอง"),
                ["0167"] = Encoding.ASCII.GetBytes("25600101"),
                ["016F"] = Encoding.ASCII.GetBytes("25700101")
            }
        };

        var data = new ThaiIdCardClient(transport).Read(includePhoto: false);
        Assert.Equal("1103700156780", data.NationalId);
        Assert.Equal("นางสาว", data.Title);
        Assert.Equal("สุดา", data.FirstName);
        Assert.Equal("ทดสอบ", data.LastName);
        Assert.Equal("1987-01-15", data.DateOfBirth);
        Assert.Equal("F", data.Gender);
        Assert.Contains("กรุงเทพมหานคร", data.Address);
    }

    [Fact]
    public void FollowsGetResponseWhenCardReturns61()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var transport = new GetResponseTransport();
        var data = new ThaiIdCardClient(transport).Read(includePhoto: false);
        Assert.Equal("1103700156780", data.NationalId);
        Assert.Equal("นาย", data.Title);
    }

    private sealed class FakeTransport : ICardTransport
    {
        public byte[] Atr { get; set; } = [0x3B, 0x78, 0x18];
        public string ReaderName => "Fake Reader";
        public Dictionary<string, byte[]> Fields { get; init; } = new();

        public (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command)
        {
            if (command.Length >= 2 && command[1] == 0xA4)
            {
                return ([], (byte)0x90, (byte)0x00);
            }

            if (command.Length >= 4 && command[0] == 0x80 && command[1] == 0xB0)
            {
                var key = $"{command[2]:X2}{command[3]:X2}";
                return Fields.TryGetValue(key, out var data)
                    ? (data, (byte)0x90, (byte)0x00)
                    : ([], (byte)0x90, (byte)0x00);
            }

            throw new InvalidOperationException(Convert.ToHexString(command));
        }

        public void Dispose()
        {
        }
    }

    private sealed class GetResponseTransport : ICardTransport
    {
        public byte[] Atr => [0x3B, 0x67, 0x00];
        public string ReaderName => "Old ATR Reader";
        private byte[] _pending = [];

        public (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command)
        {
            if (command[1] == 0xA4)
            {
                return ([], (byte)0x90, (byte)0x00);
            }

            if (command[1] == 0xC0)
            {
                Assert.Equal(0x01, command[3]);
                return (_pending, (byte)0x90, (byte)0x00);
            }

            if (command is [0x80, 0xB0, 0x00, 0x04, ..])
            {
                _pending = "1103700156780"u8.ToArray();
                return ([], (byte)0x61, (byte)_pending.Length);
            }

            if (command is [0x80, 0xB0, 0x00, 0x11, ..])
            {
                _pending = ThaiIdCardParser.ThaiEncoding.GetBytes("นาย#วิชาญ##อ่านบัตร");
                return ([], (byte)0x61, (byte)_pending.Length);
            }

            _pending = [];
            return ([], (byte)0x90, (byte)0x00);
        }

        public void Dispose()
        {
        }
    }
}
