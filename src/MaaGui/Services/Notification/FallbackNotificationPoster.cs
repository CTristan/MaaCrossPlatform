using System;
using Serilog;

namespace MaaGui.Services.Notification;

/// <summary>
/// Fallback notification poster using logging
/// </summary>
public class FallbackNotificationPoster : INotificationPoster
{
    public void Show(string title, string message)
    {
        Log.Information("Notification [{Title}]: {Message}", title, message);
    }

    public void Post(string message, string level = "Information")
    {
        Log.Information("Notification [{Level}]: {Message}", level, message);
    }

    public bool IsSupported => true;
}
