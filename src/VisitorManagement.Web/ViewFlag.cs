namespace VisitorManagement.Web;

/// <summary>
/// Reads boolean flags stored in ViewBag/ViewData.
/// A boxed <see cref="bool"/> cannot be read with <c>as bool?</c> — that always returns null.
/// </summary>
public static class ViewFlag
{
    public static bool IsOn(object? value) => value switch
    {
        bool flag => flag,
        string text when bool.TryParse(text, out var parsed) => parsed,
        int number => number != 0,
        _ => false
    };

    public static bool IsOn(object? value, bool whenMissing) =>
        value is null ? whenMissing : IsOn(value);
}
