using System.Text;
using VisitorManagement.CardReader;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if WINDOWS_TRAY
ApplicationConfiguration.Initialize();
Application.SetHighDpiMode(HighDpiMode.SystemAware);
TrayApplicationContext.Run(args);
#else
var app = CardReaderWebHost.Build(args);
var listenUrl = CardReaderWebHost.GetListenUrl(app.Configuration);
Console.WriteLine($"VisitorManagement.CardReader listening on {listenUrl}");
Console.WriteLine("Health:  GET /health");
Console.WriteLine("Status:  GET /api/status");
Console.WriteLine("Read:    GET /api/thcard?photo=true");
app.Run();
#endif
