using PCSC;
using PCSC.Exceptions;
using VisitorManagement.CardReader.Core;

namespace VisitorManagement.CardReader;

public sealed class PcscReaderHub : IPcscReaderHub
{
    public PcscProbeResult Probe()
    {
        try
        {
            using var context = ContextFactory.Instance.Establish(SCardScope.System);
            IReadOnlyList<string> readers = context.GetReaders() ?? Array.Empty<string>();
            return new PcscProbeResult(
                true,
                readers,
                readers.Count == 0
                    ? "ไม่พบเครื่องอ่านบัตร USB"
                    : $"พบเครื่องอ่าน {readers.Count} เครื่อง");
        }
        catch (Exception ex) when (ex is PCSCException or DllNotFoundException or TypeInitializationException)
        {
            var message = DescribePcscFailure(ex);
            return new PcscProbeResult(false, [], message);
        }
    }

    public IReadOnlyList<string> ListReaders() => Probe().Readers;

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
        var probe = Probe();
        if (!probe.PcscAvailable)
        {
            throw new ThaiIdCardException("pcsc_unavailable", probe.Message);
        }

        var readers = probe.Readers;
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

    private static string DescribePcscFailure(Exception ex)
    {
        if (ex is DllNotFoundException)
        {
            return "ไม่พบไลบรารี PC/SC (libpcsclite) — ติดตั้ง pcscd และ libpcsclite1";
        }

        if (ex is PCSCException pcsc)
        {
            return pcsc.SCardError switch
            {
                SCardError.NoService => "บริการ PC/SC (pcscd / Smart Card) ยังไม่ทำงาน — เปิดบริการแล้วรันโปรแกรมนี้อีกครั้ง",
                SCardError.ServiceStopped => "บริการ PC/SC หยุดทำงาน — เปิด pcscd หรือ Smart Card Service",
                SCardError.NoReadersAvailable => "ไม่พบเครื่องอ่านบัตร USB",
                _ => "เชื่อมต่อ PC/SC ไม่สำเร็จ: " + pcsc.Message
            };
        }

        return "เชื่อมต่อ PC/SC ไม่สำเร็จ: " + ex.Message;
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
        try
        {
            Atr = reader.GetAttrib(SCardAttribute.AtrString) ?? [];
        }
        catch (PCSCException ex)
        {
            throw new ThaiIdCardException("reader_error", "อ่าน ATR จากบัตรไม่สำเร็จ: " + ex.Message);
        }

        ReaderName = reader.Name;
    }

    public byte[] Atr { get; }
    public string ReaderName { get; }

    public (byte[] Data, byte Sw1, byte Sw2) Transmit(byte[] command)
    {
        try
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
        catch (ThaiIdCardException)
        {
            throw;
        }
        catch (PCSCException ex)
        {
            throw new ThaiIdCardException("read_failed", "สื่อสารกับบัตรไม่สำเร็จ: " + ex.Message);
        }
        catch (Exception ex)
        {
            throw new ThaiIdCardException("read_failed", "สื่อสารกับบัตรไม่สำเร็จ: " + ex.Message);
        }
    }

    public void Dispose()
    {
        _reader.Dispose();
        _context.Dispose();
    }
}
