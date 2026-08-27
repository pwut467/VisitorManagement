using System.Text;

namespace VisitorManagement.CardReader.Core;

public static class ThaiIdCardParser
{
    static ThaiIdCardParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static Encoding ThaiEncoding => Encoding.GetEncoding(874);

    public static string DecodeTis620(byte[]? data)
    {
        if (data is null || data.Length == 0)
        {
            return string.Empty;
        }

        var end = data.Length;
        while (end > 0 && data[end - 1] is 0x00 or 0x20)
        {
            end--;
        }

        return ThaiEncoding.GetString(data, 0, end).Trim();
    }

    public static (string Title, string FirstName, string MiddleName, string LastName) ParseName(byte[]? data)
    {
        var parts = DecodeTis620(data)
            .Split('#', StringSplitOptions.None)
            .Select(p => p.Trim())
            .ToArray();

        string At(int i) => i < parts.Length ? parts[i] : string.Empty;
        return (At(0), At(1), At(2), At(3));
    }

    public static string ParseAddress(byte[]? data)
    {
        var parts = DecodeTis620(data)
            .Split('#', StringSplitOptions.None)
            .Select(p => p.Trim())
            .ToList();

        if (parts.TrueForAll(string.IsNullOrEmpty))
        {
            return string.Empty;
        }

        var pieces = new List<string>();
        Add(parts, 0, null);
        Add(parts, 1, "หมู่");
        Add(parts, 2, "ซอย");
        Add(parts, 3, "ถนน");
        for (var i = 4; i < parts.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
            {
                pieces.Add(parts[i]);
            }
        }

        return string.Join(" ", pieces);

        void Add(List<string> source, int index, string? prefix)
        {
            if (index >= source.Count || string.IsNullOrWhiteSpace(source[index]))
            {
                return;
            }

            var value = source[index];
            if (prefix is not null && !value.Contains(prefix, StringComparison.Ordinal))
            {
                pieces.Add(prefix + " " + value);
            }
            else
            {
                pieces.Add(value);
            }
        }
    }

    public static string? ParseBuddhistDate(byte[]? data)
    {
        var raw = new string(DecodeTis620(data).Where(char.IsDigit).ToArray());
        if (raw.Length < 8 || raw.StartsWith("0000", StringComparison.Ordinal))
        {
            return null;
        }

        if (!int.TryParse(raw[..4], out var year) ||
            !int.TryParse(raw[4..6], out var month) ||
            !int.TryParse(raw[6..8], out var day))
        {
            return null;
        }

        if (year >= 2400)
        {
            year -= 543;
        }

        try
        {
            return new DateTime(year, month, day).ToString("yyyy-MM-dd");
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public static string? ParseGender(byte[]? data)
    {
        var s = DecodeTis620(data);
        if (s.StartsWith('1'))
        {
            return "M";
        }

        if (s.StartsWith('2'))
        {
            return "F";
        }

        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    public static byte[] ExtractJpeg(byte[] photoParts)
    {
        var start = IndexOf(photoParts, 0xFF, 0xD8);
        if (start < 0)
        {
            return [];
        }

        var end = LastIndexOf(photoParts, 0xFF, 0xD9);
        return end < start ? photoParts[start..] : photoParts[start..(end + 2)];
    }

    private static int IndexOf(byte[] data, byte a, byte b)
    {
        for (var i = 0; i < data.Length - 1; i++)
        {
            if (data[i] == a && data[i + 1] == b)
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastIndexOf(byte[] data, byte a, byte b)
    {
        for (var i = data.Length - 2; i >= 0; i--)
        {
            if (data[i] == a && data[i + 1] == b)
            {
                return i;
            }
        }

        return -1;
    }
}
