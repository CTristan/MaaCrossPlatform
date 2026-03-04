using System.Runtime.Versioning;
using Serilog;

namespace MaaGui.Services.Notification;

/// <summary>
/// macOS notification poster
/// </summary>
[SupportedOSPlatform("osx")]
public class MacNotificationPoster : INotificationPoster
{
    public bool IsSupported => true;

    public MacNotificationPoster()
    {
        Log.Information("MacNotificationPoster initialized");
    }

    public void Show(string title, string message)
    {
        Log.Information("macOS notification: {Title} - {Message}", title, message);
    }

    public void Post(string message, string level = "Information")
    {
        Show(level, message);
    }
}
