using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VisitorManagement.Web.Controllers;

/// <summary>
/// Same-origin proxy so the browser can reach the local CardReader agent
/// when the web UI is opened through a remote tunnel (not localhost).
/// </summary>
[Authorize]
[Route("api/card-reader")]
public class CardReaderProxyController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CardReaderProxyController> _logger;

    public CardReaderProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<CardReaderProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    private string AgentBase =>
        (_config["CardReader:AgentUrl"] ?? "http://127.0.0.1:5001").TrimEnd('/');

    [HttpGet("health")]
    [HttpGet("api/health")]
    public Task<IActionResult> Health(CancellationToken cancellationToken) =>
        ProxyGetAsync("/health", cancellationToken);

    [HttpGet("status")]
    [HttpGet("api/status")]
    public Task<IActionResult> Status(CancellationToken cancellationToken) =>
        ProxyGetAsync("/api/status", cancellationToken);

    [HttpGet("thcard")]
    [HttpGet("api/thcard")]
    public Task<IActionResult> ThCard([FromQuery] bool photo = true, CancellationToken cancellationToken = default) =>
        ProxyGetAsync("/api/thcard?photo=" + (photo ? "true" : "false"), cancellationToken, TimeSpan.FromSeconds(25));

    private async Task<IActionResult> ProxyGetAsync(string pathAndQuery, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        var client = _httpClientFactory.CreateClient("CardReaderAgent");
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout ?? TimeSpan.FromSeconds(5));

        try
        {
            using var response = await client.GetAsync(AgentBase + pathAndQuery, timeoutCts.Token);
            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (string.IsNullOrWhiteSpace(body))
            {
                return StatusCode((int)response.StatusCode, new
                {
                    ok = false,
                    error = "empty_response",
                    message = "โปรแกรมอ่านบัตรตอบกลับว่าง (HTTP " + (int)response.StatusCode + ")"
                });
            }

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = contentType
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "CardReader agent unreachable at {AgentBase}", AgentBase);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                error = "agent_unreachable",
                message = "เปิดโปรแกรมอ่านบัตรบนเครื่องเซิร์ฟเวอร์ไม่สำเร็จ — รัน src/VisitorManagement.CardReader ที่พอร์ต 5001"
            });
        }
    }
}
