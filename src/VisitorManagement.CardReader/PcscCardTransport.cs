using PCSC;
using PCSC.Exceptions;
using VisitorManagement.CardReader.Core;

namespace VisitorManagement.CardReader;

public sealed class PcscReaderHub : IPcscReaderHub
{
    public IReadOnlyList<string> ListReaders()
    {
        try
        {
            using var context = ContextFactory.Instance.Establish(SCardScope.System);
            return context.GetReaders() ?? [];
        }
        catch (Exception ex) when (ex is PCSCException or DllNotFoundException or TypeInitializationException)
        {
            return [];
        }
    }

    public bool HasCard(string readerName)
    {
        try
        {
            using var context = ContextFactory.Instance.Establish(SCardScope.System);
            var state = context.GetReaderStatus(readerName);
            return state.EventState.HasFlag(SCRState.Present) && !state.EventState.HasFlag(SCRState.Empty);
        }
        catch (PCSCException)
        {
            return false;
        }
    }

    public ICardTransport Connect(string? readerName = null)
    {
        var readers = ListReaders();
        if (readers.Count == 0)
        {
            throw new ThaiIdCardException("no_reader", "ไม่พบเครื่องอ่านบัตร USB กรุณาเสียบเครื่องอ่านและติดตั้งไดรเวอร์ PC/SC");
        }

        var chosen = readerName;
        if (string.IsNullOrWhiteSpace(chosen))
        {
            chosen = readers.FirstOrDefault(HasCard) ?? readers[0];
        }

        if (!HasCard(chosen))
        {
            throw new ThaiIdCardException("no_card", "ไม่พบบัตรในเครื่องอ่าน กรุณาเสียบบัตรประชาชน");
        }

        try
        {
            var context = ContextFactory.Instance.Establish(SCardScope.System);
            var reader = context.ConnectReader(chosen, SCardShareMode.Shared, SCardProtocol.Any);
            return new PcscCardTransport(context, reader);
        }
        catch (PCSCException ex) when (ex.SCardError == SCardError.NoSmartcard)
        {
            throw new ThaiIdCardException("no_card", "ไม่พบบัตรในเครื่องอ่าน กรุณาเสียบบัตรประชาชน");
        }
        catch (PCSCException ex)
        {
            throw new ThaiIdCardException("reader_error", "เชื่อมต่อเครื่องอ่านบัตรไม่สำเร็จ: " + ex.Message);
        }
    }
}

internal sealed class PcscCardTransport : ICardTransport
{
    private readonly ISCardContext _context;
    private readonly ICardReader _reader;

    public PcscCardTransport(ISCardContext context, ICardReader reader)
    {
        _context = context;
        _reader = reader;
        Atr = reader.GetAttrib(SCardAttribute.AtrString) ?? [];
        ReaderName = reader.Name;
    }

    public byte[] Atr { get; }
    public string ReaderName { get; }

    public (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command)
    {
        var recv = new byte[512];
        var pci = SCardPCI.GetPci(_reader.Protocol);
        var received = _reader.Transmit(pci, command, recv);
        if (received < 2)
        {
            throw new ThaiIdCardException("read_failed", "เครื่องอ่านไม่ได้ตอบกลับจากบัตร");
        }

        var sw1 = recv[received - 2];
        var sw2 = recv[received - 1];
        var data = received > 2 ? recv[..(received - 2)] : [];
        return (data, sw1, sw2);
    }

    public void Dispose()
    {
        _reader.Dispose();
        _context.Dispose();
    }
}
