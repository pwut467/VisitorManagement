using System.Text.RegularExpressions;

namespace VisitorManagement.Web.Services;

public static class ThaiNationalId
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"\D", string.Empty);
    }

    public static bool IsValid(string? value)
    {
        var id = Normalize(value);
        if (id.Length != 13 || !id.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += (id[i] - '0') * (13 - i);
        }

        var check = (11 - (sum % 11)) % 10;
        return check == id[12] - '0';
    }

    public static string Mask(string? value)
    {
        var id = Normalize(value);
        if (id.Length != 13)
        {
            return value ?? string.Empty;
        }

        return $"{id[0]}-xxxx-xxxxx-{id[10]}{id[11]}-{id[12]}";
    }

    public static string Format(string? value)
    {
        var id = Normalize(value);
        if (id.Length != 13)
        {
            return value ?? string.Empty;
        }

        return $"{id[0]}-{id[1..5]}-{id[5..10]}-{id[10..12]}-{id[12]}";
    }
}