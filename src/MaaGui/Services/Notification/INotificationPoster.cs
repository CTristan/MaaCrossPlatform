namespace MaaGui.Services.Notification;

/// <summary>
/// Notification poster interface
/// </summary>
public interface INotificationPoster
{
    /// <summary>
    /// Show a notification
    /// </summary>
    void Show(string title, string message);

    /// <summary>
    /// Post a notification (Alias for Show)
    /// </summary>
    void Post(string message, string level = "Information");

    /// <summary>
    /// Check if notifications are available
    /// </summary>
    bool IsSupported { get; }
}
