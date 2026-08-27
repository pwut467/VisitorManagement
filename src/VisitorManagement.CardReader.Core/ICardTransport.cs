namespace VisitorManagement.CardReader.Core;

public interface ICardTransport : IDisposable
{
    byte[] Atr { get; }
    string ReaderName { get; }
    (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command);
}

public sealed record PcscProbeResult(
    bool PcscAvailable,
    IReadOnlyList<string> Readers,
    string Message);

public interface IPcscReaderHub
{
    PcscProbeResult Probe();
    IReadOnlyList<string> ListReaders();
    bool HasCard(string readerName);
    ICardTransport Connect(string? readerName = null);
}
