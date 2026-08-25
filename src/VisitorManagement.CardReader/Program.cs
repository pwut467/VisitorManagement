using VisitorManagement.CardReader;
using VisitorManagement.CardReader.Core;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["CardReader:ListenUrl"] ?? "http://127.0.0.1:5001");
builder.Services.AddSingleton<IPcscReaderHub, PcscReaderHub>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "thai-id-card-reader" }));

app.MapGet("/api/status", (IPcscReaderHub hub) =>
{
    var readers = hub.ListReaders();
    var withCard = readers.Where(hub.HasCard).ToArray();
    return Results.Ok(new
    {
        ok = true,
        agent = true,
        readers,
        hasReader = readers.Count > 0,
        hasCard = withCard.Length > 0,
        message = readers.Count == 0
            ? "ไม่พบเครื่องอ่านบัตร USB"
            : withCard.Length == 0
                ? "พบเครื่องอ่านแล้ว กรุณาเสียบบัตรประชาชน"
                : "พร้อมอ่านบัตร"
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
            "no_card" => StatusCodes.Status409Conflict,
            "not_thai_id" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Json(new { ok = false, error = ex.ErrorCode, message = ex.Message }, statusCode: status);
    }
});

app.Run();
