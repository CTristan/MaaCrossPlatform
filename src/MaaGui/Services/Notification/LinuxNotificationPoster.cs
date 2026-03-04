using System.Runtime.Versioning;
using Serilog;

namespace MaaGui.Services.Notification;

/// <summary>
/// Linux notification poster
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxNotificationPoster : INotificationPoster
{
    public bool IsSupported => true;

    public LinuxNotificationPoster()
    {
        Log.Information("LinuxNotificationPoster initialized");
    }

    public void Show(string title, string message)
    {
        Log.Information("Linux notification: {Title} - {Message}", title, message);
    }

    public void Post(string message, string level = "Information")
    {
        Show(level, message);
    }
}
