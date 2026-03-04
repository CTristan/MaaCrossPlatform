using System.Runtime.Versioning;
using Serilog;

namespace MaaGui.Services.Notification;

/// <summary>
/// Windows notification poster
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsNotificationPoster : INotificationPoster
{
    public bool IsSupported => true;

    public WindowsNotificationPoster()
    {
        Log.Information("WindowsNotificationPoster initialized");
    }

    public void Show(string title, string message)
    {
        Log.Information("Windows notification: {Title} - {Message}", title, message);
    }

    public void Post(string message, string level = "Information")
    {
        Show(level, message);
    }
}
