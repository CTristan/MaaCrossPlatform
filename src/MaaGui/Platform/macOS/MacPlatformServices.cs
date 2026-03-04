using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MaaGui.Platform.macOS;

/// <summary>
/// macOS-specific platform services
/// </summary>
[SupportedOSPlatform("osx")]
public class MacPlatformServices : IPlatformServices
{
    private const string kIOPMAssertionTypePreventUserIdleSystemSleep = "PreventUserIdleSystemSleep";
    private const string kIOPMAssertionName = "MaaAssistantArknights task execution";
    private const nint kCFAllocatorDefault = 0;
    private const uint kCFStringEncodingUTF8 = 0x08000100;

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    private static extern uint IOPMAssertionCreateWithName(
        nint assertionType,
        uint assertionLevel,
        nint assertionName,
        out nint assertionId);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    private static extern uint IOPMAssertionRelease(nint assertionId);

    private nint _assertionId = 0;
    private bool _sleepPrevented;

    public void PreventSleep()
    {
        if (!_sleepPrevented)
        {
            var typePtr = CFStringCreateWithCString(kCFAllocatorDefault, kIOPMAssertionTypePreventUserIdleSystemSleep, kCFStringEncodingUTF8);
            var namePtr = CFStringCreateWithCString(kCFAllocatorDefault, kIOPMAssertionName, kCFStringEncodingUTF8);
            var result = IOPMAssertionCreateWithName(typePtr, 255, namePtr, out _assertionId);

            CFRelease(typePtr);
            CFRelease(namePtr);

            if (result == 0)
            {
                _sleepPrevented = true;
            }
        }
    }

    public void AllowSleep()
    {
        if (_sleepPrevented && _assertionId != 0)
        {
            IOPMAssertionRelease(_assertionId);
            _assertionId = 0;
            _sleepPrevented = false;
        }
    }

    public string GetNativeLibraryPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var libPath = Path.Combine(appDir, "libMaaCore.dylib");
        return File.Exists(libPath) ? libPath : Path.Combine(appDir, "lib", "libMaaCore.dylib");
    }

    public string GetDefaultAdbPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var adbPath = Path.Combine(appDir, "adb");
        return File.Exists(adbPath) ? adbPath : "/usr/local/bin/adb";
    }

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFStringCreateWithCString(nint allocator, string cStr, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);
}
