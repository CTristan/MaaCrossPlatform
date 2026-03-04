using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MaaGui.Platform.Windows;

/// <summary>
/// Windows-specific platform services
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsPlatformServices : IPlatformServices
{
    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private bool _sleepPrevented;

    public void PreventSleep()
    {
        if (!_sleepPrevented)
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
            _sleepPrevented = true;
        }
    }

    public void AllowSleep()
    {
        if (_sleepPrevented)
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            _sleepPrevented = false;
        }
    }

    public string GetNativeLibraryPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var libPath = Path.Combine(appDir, "MaaCore.dll");
        return File.Exists(libPath) ? libPath : Path.Combine(appDir, "lib", "MaaCore.dll");
    }

    public string GetDefaultAdbPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var adbPath = Path.Combine(appDir, "adb.exe");
        return File.Exists(adbPath) ? adbPath : "adb";
    }
}
