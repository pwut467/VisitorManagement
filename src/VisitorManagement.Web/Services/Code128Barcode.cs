using System.Globalization;
using System.Net;
using System.Text;

namespace VisitorManagement.Web.Services;

public static class Code128Barcode
{
    private static readonly string[] Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222",
        "122213", "122312", "132212", "221213", "221312", "231212",
        "112232", "122132", "122231", "113222", "123122", "123221",
        "223211", "221132", "221231", "213212", "223112", "312131",
        "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321",
        "112313", "132113", "132311", "211313", "231113", "231311",
        "112133", "112331", "132131", "113123", "113321", "133121",
        "313121", "211331", "231131", "213113", "213311", "213131",
        "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124",
        "121421", "141122", "141221", "112214", "112412", "122114",
        "122411", "142112", "142211", "241211", "221114", "413111",
        "241112", "134111", "111242", "121142", "121241", "114212",
        "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113",
        "114311", "411113", "411311", "113141", "114131", "311141",
        "411131", "211412", "211214", "211232", "2331112"
    ];

    private const int StartB = 104;
    private const int Stop = 106;

    public static string Svg(string value, int barHeight = 36, int module = 2, bool includeLabel = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ต้องระบุข้อความสำหรับบาร์โค้ด", nameof(value));
        }

        var codes = Encode(value);
        var quiet = 10 * module;
        var width = quiet * 2;
        foreach (var code in codes)
        {
            foreach (var ch in Patterns[code])
            {
                width += (ch - '0') * module;
            }
        }

        var textHeight = includeLabel ? 14 : 0;
        var height = barHeight + textHeight + (includeLabel ? 6 : 4);
        var sb = new StringBuilder();
        // preserveAspectRatio keeps module widths proportional when CSS scales the SVG for 80mm badges.
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" preserveAspectRatio=\"xMidYMid meet\" role=\"img\" aria-label=\"barcode {WebUtility.HtmlEncode(value)}\">");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");

        var x = quiet;
        var bar = true;
        foreach (var code in codes)
        {
            foreach (var ch in Patterns[code])
            {
                var w = (ch - '0') * module;
                if (bar)
                {
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<rect x=\"{x}\" y=\"0\" width=\"{w}\" height=\"{barHeight}\" fill=\"#000\"/>");
                }
                x += w;
                bar = !bar;
            }
        }

        if (includeLabel)
        {
            var label = WebUtility.HtmlEncode(value);
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{width / 2}\" y=\"{barHeight + 12}\" text-anchor=\"middle\" font-family=\"monospace\" font-size=\"11\">{label}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    public static IReadOnlyList<int> Encode(string value)
    {
        var codes = new List<int> { StartB };
        var checksum = StartB;
        var position = 1;
        foreach (var ch in value)
        {
            if (ch < 32 || ch > 126)
            {
                throw new ArgumentException($"บาร์โค้ดไม่รองรับตัวอักษร '{ch}'", nameof(value));
            }

            var code = ch - 32;
            codes.Add(code);
            checksum += code * position;
            position++;
        }

        codes.Add(checksum % 103);
        codes.Add(Stop);
        return codes;
    }
}
