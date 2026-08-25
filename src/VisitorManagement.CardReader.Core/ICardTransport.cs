namespace VisitorManagement.CardReader.Core;

public interface ICardTransport : IDisposable
{
    byte[] Atr { get; }
    string ReaderName { get; }
    (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command);
}

public interface IPcscReaderHub
{
    IReadOnlyList<string> ListReaders();
    bool HasCard(string readerName);
    ICardTransport Connect(string? readerName = null);
}
