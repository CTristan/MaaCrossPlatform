namespace MaaGui.Platform;

/// <summary>
/// Platform-specific services interface
/// </summary>
public interface IPlatformServices
{
    /// <summary>
    /// Prevent system sleep during task execution
    /// </summary>
    void PreventSleep();

    /// <summary>
    /// Allow system sleep (restore normal behavior)
    /// </summary>
    void AllowSleep();

    /// <summary>
    /// Get the native MaaCore library path for the current platform
    /// </summary>
    string GetNativeLibraryPath();

    /// <summary>
    /// Get the default ADB path for the current platform
    /// </summary>
    string GetDefaultAdbPath();
}
