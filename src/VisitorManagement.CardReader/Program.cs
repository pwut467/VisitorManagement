using System.Text;
using VisitorManagement.CardReader;
using VisitorManagement.CardReader.Core;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["CardReader:ListenUrl"] ?? "http://127.0.0.1:5001");
builder.Services.AddSingleton<IPcscReaderHub, PcscReaderHub>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Chrome Private Network Access: public/LAN web pages calling http://127.0.0.1:5001
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Access-Control-Allow-Private-Network"] = "true";

    if (HttpMethods.IsOptions(ctx.Request.Method))
    {
        var origin = ctx.Request.Headers.Origin.FirstOrDefault() ?? "*";
        ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "thai-id-card-reader" }));

app.MapGet("/api/status", (IPcscReaderHub hub) =>
{
    var probe = hub.Probe();
    var readers = probe.Readers;
    var withCard = readers.Where(hub.HasCard).ToArray();
    var message = !probe.PcscAvailable
        ? probe.Message
        : readers.Count == 0
            ? "ไม่พบเครื่องอ่านบัตร USB — เสียบเครื่องอ่านที่เครื่องนี้แล้วติดตั้งไดรเวอร์ PC/SC"
            : withCard.Length == 0
                ? "พบเครื่องอ่านแล้ว กรุณาเสียบบัตรประชาชน"
                : "พร้อมอ่านบัตร";

    return Results.Ok(new
    {
        ok = true,
        agent = true,
        pcscAvailable = probe.PcscAvailable,
        readers,
        hasReader = readers.Count > 0,
        hasCard = withCard.Length > 0,
        message
    });
});

app.MapGet("/api/thcard", (IPcscReaderHub hub, bool photo = true) =>
{
    try
    {
        using var transport = hub.Connect();
        var data = new ThaiIdCardClient(transport).Read(photo);
        return Results.Ok(new
        {
            ok = true,
            data.NationalId,
            nationalId = data.NationalId,
            data.Title,
            title = data.Title,
            data.FirstName,
            firstName = data.FirstName,
            data.MiddleName,
            middleName = data.MiddleName,
            data.LastName,
            lastName = data.LastName,
            data.Address,
            address = data.Address,
            data.DateOfBirth,
            dateOfBirth = data.DateOfBirth,
            data.Gender,
            gender = data.Gender,
            data.IssueDate,
            data.ExpireDate,
            data.Issuer,
            photo = data.PhotoDataUrl,
            reader = data.ReaderName
        });
    }
    catch (ThaiIdCardException ex)
    {
        var status = ex.ErrorCode switch
        {
            "no_reader" => StatusCodes.Status503ServiceUnavailable,
            "pcsc_unavailable" => StatusCodes.Status503ServiceUnavailable,
            "no_card" => StatusCodes.Status409Conflict,
            "not_thai_id" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Json(new { ok = false, error = ex.ErrorCode, message = ex.Message }, statusCode: status);
    }
});

Console.WriteLine("VisitorManagement.CardReader listening on http://127.0.0.1:5001");
Console.WriteLine("Health:  GET /health");
Console.WriteLine("Status:  GET /api/status");
Console.WriteLine("Read:    GET /api/thcard?photo=true");
app.Run();
