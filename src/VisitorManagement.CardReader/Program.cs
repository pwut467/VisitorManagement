using System.Text;
using System.Text.Json;
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

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (ctx.Response.HasStarted)
        {
            throw;
        }

        ctx.Response.Clear();
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            ok = false,
            error = "unhandled",
            message = "โปรแกรมอ่านบัตรขัดข้อง: " + ex.Message
        }));
    }
});

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "thai-id-card-reader" }));
app.MapGet("/status", (IPcscReaderHub hub) => StatusPayload(hub));
app.MapGet("/api/status", (IPcscReaderHub hub) => StatusPayload(hub));

app.MapGet("/thcard", (IPcscReaderHub hub, bool photo = true) => ReadCard(hub, photo));
app.MapGet("/api/thcard", (IPcscReaderHub hub, bool photo = true) => ReadCard(hub, photo));

Console.WriteLine("VisitorManagement.CardReader listening on http://127.0.0.1:5001");
Console.WriteLine("Health:  GET /health");
Console.WriteLine("Status:  GET /api/status");
Console.WriteLine("Read:    GET /api/thcard?photo=true");
app.Run();

static IResult StatusPayload(IPcscReaderHub hub)
{
    try
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

        return Results.Json(new
        {
            ok = true,
            agent = true,
            pcscAvailable = probe.PcscAvailable,
            readers,
            hasReader = readers.Count > 0,
            hasCard = withCard.Length > 0,
            message
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            ok = false,
            error = "status_failed",
            message = "ตรวจสถานะเครื่องอ่านไม่สำเร็จ: " + ex.Message
        }, statusCode: StatusCodes.Status500InternalServerError);
    }
}

static IResult ReadCard(IPcscReaderHub hub, bool photo)
{
    try
    {
        using var transport = hub.Connect();
        var data = new ThaiIdCardClient(transport).Read(photo);
        return Results.Json(new
        {
            ok = true,
            nationalId = data.NationalId,
            title = data.Title,
            firstName = data.FirstName,
            middleName = data.MiddleName,
            lastName = data.LastName,
            address = data.Address,
            dateOfBirth = data.DateOfBirth,
            gender = data.Gender,
            issueDate = data.IssueDate,
            expireDate = data.ExpireDate,
            issuer = data.Issuer,
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
    catch (Exception ex)
    {
        return Results.Json(new
        {
            ok = false,
            error = "read_failed",
            message = "อ่านบัตรไม่สำเร็จ: " + ex.Message
        }, statusCode: StatusCodes.Status500InternalServerError);
    }
}
