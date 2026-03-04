using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using MaaGui.Platform;

namespace MaaGui.Services.MaaInterop;

/// <summary>
/// MaaCore C API implementation with cross-platform native library loading
/// </summary>
public partial class MaaService : IMaaService, IDisposable
{
    private readonly IPlatformServices _platformServices;
    private nint _libraryHandle = 0;
    private bool _disposed = false;
    private static readonly object _versionLock = new object();

    public MaaService(IPlatformServices platformServices)
    {
        _platformServices = platformServices;

        // Set up DllImport resolver for cross-platform library loading
        NativeLibrary.SetDllImportResolver(typeof(MaaService).Assembly, ResolveMaaCoreLibrary);
    }

    private nint ResolveMaaCoreLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Equals("MaaCore", StringComparison.OrdinalIgnoreCase))
        {
            return 0; // Use default resolution for other libraries
        }

        var libPath = _platformServices.GetNativeLibraryPath();
        if (File.Exists(libPath))
        {
            _libraryHandle = NativeLibrary.Load(libPath);
            return _libraryHandle;
        }

        // Try default library name for each platform
        var platformLibName = GetPlatformLibName();
        _libraryHandle = NativeLibrary.Load(platformLibName);
        return _libraryHandle;
    }

    private string GetPlatformLibName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "MaaCore.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "libMaaCore.dylib";
        }
        else
        {
            return "libMaaCore.so";
        }
    }

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstSetUserDir(string path);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstLoadResource(string path);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstSetStaticOption(AsstStaticOptionKey key, string value);

    [LibraryImport("MaaCore")]
    public static partial nint AsstCreate();

    [LibraryImport("MaaCore")]
    public static partial nint AsstCreateEx(AsstApiCallback callback, nint customArg);

    [LibraryImport("MaaCore")]
    public static partial void AsstDestroy(nint handle);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstSetInstanceOption(nint handle, AsstInstanceOptionKey key, string value);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AsstAppendTask(nint handle, string type, string @params);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstSetTaskParams(nint handle, int id, string @params);

    [LibraryImport("MaaCore")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstStart(nint handle);

    [LibraryImport("MaaCore")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstStop(nint handle);

    [LibraryImport("MaaCore")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstRunning(nint handle);

    [LibraryImport("MaaCore")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstConnected(nint handle);

    [LibraryImport("MaaCore")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AsstBackToHome(nint handle);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AsstAsyncConnect(nint handle, string adb_path, string address, string config, [MarshalAs(UnmanagedType.U1)] bool block);

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AsstSetConnectionExtras(string name, string extras);

    [LibraryImport("MaaCore")]
    public static partial int AsstAsyncClick(nint handle, int x, int y, [MarshalAs(UnmanagedType.U1)] bool block);

    [LibraryImport("MaaCore")]
    public static partial int AsstAsyncScreencap(nint handle, [MarshalAs(UnmanagedType.U1)] bool block);

    [LibraryImport("MaaCore")]
    public static partial int AsstGetImage(nint handle, nint buff, int buff_size);

    [LibraryImport("MaaCore")]
    public static partial int AsstGetImageBgr(nint handle, nint buff, int buff_size);

    [LibraryImport("MaaCore")]
    public static partial int AsstGetUUID(nint handle, nint buff, int buff_size);

    [LibraryImport("MaaCore")]
    public static partial int AsstGetTasksList(nint handle, nint buff, int buff_size);

    [LibraryImport("MaaCore")]
    public static partial int AsstGetNullSize();

    [LibraryImport("MaaCore")]
    public static partial nint AsstGetVersion();

    [LibraryImport("MaaCore", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AsstLog(string level, string message);

#if WINDOWS
    [LibraryImport("MaaCore")]
    public static partial int AsstAsyncAttachWindow(nint handle, nint hwnd, ulong screencap_method, ulong mouse_method, ulong keyboard_method, [MarshalAs(UnmanagedType.U1)] bool block);
#endif

    // IMaaService Implementation

    public bool SetUserDir(string path) => AsstSetUserDir(path);
    public bool LoadResource(string path) => AsstLoadResource(path);
    public bool SetStaticOption(AsstStaticOptionKey key, string value) => AsstSetStaticOption(key, value);
    public nint Create() => AsstCreate();
    public nint CreateEx(AsstApiCallback callback, nint customArg) => AsstCreateEx(callback, customArg);
    public void Destroy(nint handle) => AsstDestroy(handle);
    public bool SetInstanceOption(nint handle, AsstInstanceOptionKey key, string value) => AsstSetInstanceOption(handle, key, value);
    public int AppendTask(nint handle, string type, string @params) => AsstAppendTask(handle, type, @params);
    public bool SetTaskParams(nint handle, int taskId, string @params) => AsstSetTaskParams(handle, taskId, @params);
    public bool Start(nint handle) => AsstStart(handle);
    public bool Stop(nint handle) => AsstStop(handle);
    public bool Running(nint handle) => AsstRunning(handle);
    public bool Connected(nint handle) => AsstConnected(handle);
    public bool BackToHome(nint handle) => AsstBackToHome(handle);
    public int AsyncConnect(nint handle, string adbPath, string address, string config, bool block) => AsstAsyncConnect(handle, adbPath, address, config, block);
    public void SetConnectionExtras(string name, string extras) => AsstSetConnectionExtras(name, extras);
    public int AsyncClick(nint handle, int x, int y, bool block) => AsstAsyncClick(handle, x, y, block);
    public int AsyncScreencap(nint handle, bool block) => AsstAsyncScreencap(handle, block);

    public int GetImage(nint handle, byte[] buffer, int bufferSize)
    {
        if (buffer == null || buffer.Length == 0)
            return 0;

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                return AsstGetImage(handle, new nint(ptr), bufferSize);
            }
        }
    }

    public int GetImageBgr(nint handle, byte[] buffer, int bufferSize)
    {
        if (buffer == null || buffer.Length == 0)
            return 0;

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                return AsstGetImageBgr(handle, new nint(ptr), bufferSize);
            }
        }
    }

    public string GetUUID(nint handle)
    {
        var bufferSize = 256;
        var buffer = new byte[bufferSize];

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                var size = AsstGetUUID(handle, new nint(ptr), bufferSize);
                if (size > 0)
                {
                    if (size > bufferSize)
                    {
                        var largerBuffer = new byte[size];
                        fixed (byte* largerPtr = largerBuffer)
                        {
                            size = AsstGetUUID(handle, new nint(largerPtr), (int)size);
                            return System.Text.Encoding.UTF8.GetString(largerBuffer, 0, (int)size - 1);
                        }
                    }
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)size - 1);
                }
            }
        }

        return string.Empty;
    }

    public int[] GetTasksList(nint handle)
    {
        var nullSize = AsstGetNullSize();
        var bufferSize = 256;
        var buffer = new int[bufferSize];

        unsafe
        {
            fixed (int* ptr = buffer)
            {
                var size = AsstGetTasksList(handle, new nint(ptr), bufferSize);
                if (size > 0)
                {
                    if (size > bufferSize)
                    {
                        var largerBuffer = new int[size];
                        fixed (int* largerPtr = largerBuffer)
                        {
                            size = AsstGetTasksList(handle, new nint(largerPtr), (int)size);
                            var result = new int[size];
                            Array.Copy(largerBuffer, 0, result, 0, (int)size);
                            return result;
                        }
                    }
                    else
                    {
                        var result = new int[size];
                        Array.Copy(buffer, 0, result, 0, (int)size);
                        return result;
                    }
                }
            }
        }

        return Array.Empty<int>();
    }

    public int GetNullSize() => AsstGetNullSize();

    public string GetVersion()
    {
        lock (_versionLock)
        {
            var ptr = AsstGetVersion();
            return ptr == 0 ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }
    }

    public void Log(string level, string message) => AsstLog(level, message);

#if WINDOWS
    public int AsyncAttachWindow(nint handle, nint hwnd, ulong screencapMethod, ulong mouseMethod, ulong keyboardMethod, bool block)
        => AsstAsyncAttachWindow(handle, hwnd, screencapMethod, mouseMethod, keyboardMethod, block);
#else
    public int AsyncAttachWindow(nint handle, nint hwnd, ulong screencapMethod, ulong mouseMethod, ulong keyboardMethod, bool block)
        => throw new PlatformNotSupportedException("AsyncAttachWindow is only supported on Windows");
#endif

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_libraryHandle != 0)
            {
                NativeLibrary.Free(_libraryHandle);
                _libraryHandle = 0;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
