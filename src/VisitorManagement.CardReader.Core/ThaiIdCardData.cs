namespace VisitorManagement.CardReader.Core;

public sealed class ThaiIdCardData
{
    public string NationalId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string MiddleName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? IssueDate { get; init; }
    public string? ExpireDate { get; init; }
    public string? Issuer { get; init; }
    public byte[]? PhotoJpeg { get; init; }
    public string? ReaderName { get; init; }

    public string? PhotoDataUrl =>
        PhotoJpeg is { Length: > 0 }
            ? "data:image/jpeg;base64," + Convert.ToBase64String(PhotoJpeg)
            : null;
}
