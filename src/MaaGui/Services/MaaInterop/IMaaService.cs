using System.Runtime.InteropServices;

namespace MaaGui.Services.MaaInterop;

/// <summary>
/// MaaCore C API interface
/// </summary>
public interface IMaaService
{
    /// <summary>
    /// Set the user directory for MaaCore
    /// </summary>
    bool SetUserDir(string path);

    /// <summary>
    /// Load resources from the specified path
    /// </summary>
    bool LoadResource(string path);

    /// <summary>
    /// Set a static option (global option)
    /// </summary>
    bool SetStaticOption(AsstStaticOptionKey key, string value);

    /// <summary>
    /// Create a MaaCore instance
    /// </summary>
    nint Create();

    /// <summary>
    /// Create a MaaCore instance with callback
    /// </summary>
    nint CreateEx(AsstApiCallback callback, nint customArg);

    /// <summary>
    /// Destroy a MaaCore instance
    /// </summary>
    void Destroy(nint handle);

    /// <summary>
    /// Set an instance-specific option
    /// </summary>
    bool SetInstanceOption(nint handle, AsstInstanceOptionKey key, string value);

    /// <summary>
    /// Append a task to the task queue
    /// </summary>
    int AppendTask(nint handle, string type, string @params);

    /// <summary>
    /// Set task parameters
    /// </summary>
    bool SetTaskParams(nint handle, int taskId, string @params);

    /// <summary>
    /// Start task execution
    /// </summary>
    bool Start(nint handle);

    /// <summary>
    /// Stop task execution
    /// </summary>
    bool Stop(nint handle);

    /// <summary>
    /// Check if tasks are running
    /// </summary>
    bool Running(nint handle);

    /// <summary>
    /// Check if connected to a device
    /// </summary>
    bool Connected(nint handle);

    /// <summary>
    /// Go back to home screen
    /// </summary>
    bool BackToHome(nint handle);

    /// <summary>
    /// Async connect to a device
    /// </summary>
    int AsyncConnect(nint handle, string adbPath, string address, string config, bool block);

    /// <summary>
    /// Set connection extras
    /// </summary>
    void SetConnectionExtras(string name, string extras);

    /// <summary>
    /// Async click at specified coordinates
    /// </summary>
    int AsyncClick(nint handle, int x, int y, bool block);

    /// <summary>
    /// Async screen capture
    /// </summary>
    int AsyncScreencap(nint handle, bool block);

    /// <summary>
    /// Get captured image data
    /// </summary>
    int GetImage(nint handle, byte[] buffer, int bufferSize);

    /// <summary>
    /// Get captured image data in BGR format
    /// </summary>
    int GetImageBgr(nint handle, byte[] buffer, int bufferSize);

    /// <summary>
    /// Get device UUID
    /// </summary>
    string GetUUID(nint handle);

    /// <summary>
    /// Get the list of task IDs
    /// </summary>
    int[] GetTasksList(nint handle);

    /// <summary>
    /// Get the null pointer size for MaaCore
    /// </summary>
    int GetNullSize();

    /// <summary>
    /// Get MaaCore version string
    /// </summary>
    string GetVersion();

    /// <summary>
    /// Log a message
    /// </summary>
    void Log(string level, string message);

#if WINDOWS
    /// <summary>
    /// Async attach to a Win32 window
    /// </summary>
    int AsyncAttachWindow(nint handle, nint hwnd, ulong screencapMethod, ulong mouseMethod, ulong keyboardMethod, bool block);
#endif
}

/// <summary>
/// Callback delegate for MaaCore
/// </summary>
public delegate void AsstApiCallback(int msg, string detailsJson, nint customArg);

/// <summary>
/// Static option keys (global settings)
/// </summary>
public enum AsstStaticOptionKey : int
{
    /// <summary>
    /// Minitouch enable/disable
    /// </summary>
    MinitouchEnabled = 0,

    /// <summary>
    /// Touch mode: adb, minitouch, maatouch, MacPlayTools
    /// </summary>
    TouchMode = 1,

    /// <summary>
    /// Deployment with pause
    /// </summary>
    DeploymentWithPause = 2,

    /// <summary>
    /// ADB lite enable/disable
    /// </summary>
    AdbLiteEnabled = 3,
}

/// <summary>
/// Instance option keys (per-instance settings)
/// </summary>
public enum AsstInstanceOptionKey : int
{
    /// <summary>
    /// Working directory
    /// </summary>
    WorkingDir = 0,

    /// <summary>
    /// Temp directory
    /// </summary>
    TempDir = 1,

    /// <summary>
    /// ADB reconnect count
    /// </summary>
    AdbReconnectCount = 2,

    /// <summary>
    /// ADB reconnect delay (ms)
    /// </summary>
    AdbReconnectDelay = 3,

    /// <summary>
    /// Task delay
    /// </summary>
    TaskDelay = 4,
}
