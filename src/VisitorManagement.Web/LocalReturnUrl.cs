namespace VisitorManagement.Web;

/// <summary>
/// Resolves a safe in-app URL for "back" links on visit details.
/// Prefers an explicit <c>returnUrl</c>, then the local Referer, then a fallback.
/// </summary>
public static class LocalReturnUrl
{
    public static string Resolve(string? returnUrl, string? referer, Func<string?, bool> isLocalUrl, string fallback)
    {
        if (IsUsable(returnUrl, isLocalUrl))
        {
            return returnUrl!;
        }

        var fromReferer = PathAndQuery(referer);
        if (IsUsable(fromReferer, isLocalUrl) && !IsSelfOrNestedVisitPage(fromReferer!))
        {
            return fromReferer!;
        }

        return fallback;
    }

    public static bool IsUsable(string? url, Func<string?, bool> isLocalUrl) =>
        !string.IsNullOrWhiteSpace(url) && isLocalUrl(url);

    private static string? PathAndQuery(string? referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
        {
            return null;
        }

        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri))
        {
            return uri.PathAndQuery;
        }

        return referer.StartsWith('/') ? referer : null;
    }

    public static bool IsSelfOrNestedVisitPage(string pathAndQuery)
    {
        var path = pathAndQuery.Split('?', 2)[0];
        return Matches(path, "/Visits/Details")
            || Matches(path, "/Visits/Badge")
            || Matches(path, "/Visits/Qr")
            || Matches(path, "/Account/Login");
    }

    private static bool Matches(string path, string prefix) =>
        path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}
