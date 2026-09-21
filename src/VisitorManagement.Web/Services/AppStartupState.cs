namespace VisitorManagement.Web.Services;

/// <summary>
/// Tracks whether local SQL bootstrap succeeded. When false, the app still listens
/// but only serves the database help page (avoids IIS HTTP 500.30 on startup crash).
/// </summary>
public sealed class AppStartupState
{
    public bool IsDatabaseReady { get; private set; }

    public string? FailureMessage { get; private set; }

    public string? FailureLogPath { get; private set; }

    public void MarkReady()
    {
        IsDatabaseReady = true;
        FailureMessage = null;
        FailureLogPath = null;
    }

    public void MarkFailed(string message, string? logPath = null)
    {
        IsDatabaseReady = false;
        FailureMessage = message;
        FailureLogPath = logPath;
    }
}
