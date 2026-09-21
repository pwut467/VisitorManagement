using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Middleware;

/// <summary>
/// When SQL bootstrap failed, keep the process alive and route users to a help page
/// instead of crashing with IIS HTTP Error 500.30.
/// </summary>
public sealed class DatabaseReadyMiddleware
{
    private static readonly PathString DatabasePath = new("/Home/Database");

    private readonly RequestDelegate _next;

    public DatabaseReadyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppStartupState startupState)
    {
        if (startupState.IsDatabaseReady)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (path.StartsWithSegments(DatabasePath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.Redirect(DatabasePath.Value!);
    }
}
