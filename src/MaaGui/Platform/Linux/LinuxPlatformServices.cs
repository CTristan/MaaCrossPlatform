using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace MaaGui.Platform.Linux;

/// <summary>
/// Linux-specific platform services
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxPlatformServices : IPlatformServices
{
    private Process? _inhibitProcess;
    private bool _sleepPrevented;

    public void PreventSleep()
    {
        if (!_sleepPrevented)
        {
            try
            {
                // Try systemd-inhibit first
                var startInfo = new ProcessStartInfo
                {
                    FileName = "systemd-inhibit",
                    Arguments = "--what=sleep --who=\"MaaAssistantArknights\" --why=\"Task execution in progress\" sleep infinity",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var inhibitProcess = new Process { StartInfo = startInfo };
                if (inhibitProcess.Start())
                {
                    _inhibitProcess = inhibitProcess;
                    _sleepPrevented = true;
                }
                else
                {
                    inhibitProcess.Dispose();
                }
            }
            catch
            {
                // systemd-inhibit not available, cannot prevent sleep
            }
        }
    }

    public void AllowSleep()
    {
        if (_sleepPrevented && _inhibitProcess != null)
        {
            try
            {
                if (!_inhibitProcess.HasExited)
                {
                    _inhibitProcess.Kill();
                }
            }
            catch { }
            finally
            {
                _inhibitProcess?.Dispose();
                _inhibitProcess = null;
                _sleepPrevented = false;
            }
        }
    }

    public string GetNativeLibraryPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var libPath = Path.Combine(appDir, "libMaaCore.so");
        return File.Exists(libPath) ? libPath : Path.Combine(appDir, "lib", "libMaaCore.so");
    }

    public string GetDefaultAdbPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var adbPath = Path.Combine(appDir, "adb");
        return File.Exists(adbPath) ? adbPath : "/usr/bin/adb";
    }
}
