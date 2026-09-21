#if WINDOWS_TRAY
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace VisitorManagement.CardReader;

/// <summary>
/// Hosts the CardReader HTTP agent in the Windows system tray (no console window).
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string MutexName = @"Local\VisitorManagement.CardReader.Tray";

    private readonly NotifyIcon _tray;
    private readonly WebApplication _app;
    private readonly string _listenUrl;
    private readonly CancellationTokenSource _cts = new();
    private readonly Icon _ownedIcon;
    private Task? _runTask;

    private TrayApplicationContext(WebApplication app, string listenUrl)
    {
        _app = app;
        _listenUrl = listenUrl.TrimEnd('/');
        _ownedIcon = CreateTrayIcon();

        var menu = new ContextMenuStrip();
        menu.Items.Add("สถานะเครื่องอ่าน", null, async (_, _) => await ShowStatusAsync());
        menu.Items.Add("เปิดหน้า Health", null, (_, _) => OpenUrl(_listenUrl + "/health"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("ออกจากโปรแกรม", null, (_, _) => Exit());

        _tray = new NotifyIcon
        {
            Icon = _ownedIcon,
            Visible = true,
            Text = "SK Visitor — อ่านบัตรประชาชน",
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += async (_, _) => await ShowStatusAsync();

        _runTask = Task.Run(async () =>
        {
            try
            {
                await _app.StartAsync(_cts.Token);
                _tray.ShowBalloonTip(
                    3000,
                    "SK Visitor Card Reader",
                    $"ทำงานที่ {_listenUrl}\nคลิกขวาที่ไอคอนใน System Tray เพื่อจัดการ",
                    ToolTipIcon.Info);
                await _app.WaitForShutdownAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "เริ่มโปรแกรมอ่านบัตรไม่สำเร็จ:\n" + ex.Message,
                    "SK Visitor Card Reader",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ExitThread();
            }
        });
    }

    public static void Run(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "โปรแกรมอ่านบัตรกำลังทำงานอยู่ใน System Tray แล้ว",
                "SK Visitor Card Reader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var app = CardReaderWebHost.Build(args);
        var url = CardReaderWebHost.GetListenUrl(app.Configuration);
        Application.Run(new TrayApplicationContext(app, url));
    }

    private async Task ShowStatusAsync()
    {
        try
        {
            using var scope = _app.Services.CreateScope();
            var hub = scope.ServiceProvider.GetRequiredService<VisitorManagement.CardReader.Core.IPcscReaderHub>();
            var probe = hub.Probe();
            var withCard = probe.Readers.Count(hub.HasCard);
            var text =
                $"URL: {_listenUrl}\n" +
                $"PC/SC: {(probe.PcscAvailable ? "พร้อม" : "ไม่พร้อม")}\n" +
                $"เครื่องอ่าน: {probe.Readers.Count} เครื่อง\n" +
                $"มีบัตร: {withCard}\n" +
                probe.Message;

            MessageBox.Show(text, "สถานะเครื่องอ่านบัตร", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "สถานะเครื่องอ่านบัตร", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        await Task.CompletedTask;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private void Exit()
    {
        _tray.Visible = false;
        _cts.Cancel();
        try
        {
            _app.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }

        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(3));
        }
        catch
        {
            // ignore
        }

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _ownedIcon.Dispose();
            _cts.Dispose();
            _ = _app.DisposeAsync().AsTask();
        }

        base.Dispose(disposing);
    }

    private static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(15, 118, 110));
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var pen = new Pen(Color.White, 1.5f);
            g.DrawEllipse(pen, 4, 4, 8, 8);
            g.DrawLine(pen, 8, 8, 8, 12);
        }

        var handle = bitmap.GetHicon();
        using var temp = Icon.FromHandle(handle);
        var clone = (Icon)temp.Clone();
        DestroyIcon(handle);
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
#endif
