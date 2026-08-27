namespace VisitorManagement.CardReader.Core;

public sealed class ThaiIdCardException : Exception
{
    public string ErrorCode { get; }

    public ThaiIdCardException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
