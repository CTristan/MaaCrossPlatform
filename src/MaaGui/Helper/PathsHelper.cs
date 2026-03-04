using System;
using System.IO;

namespace MaaGui.Helper;

/// <summary>
/// Helper class for managing commonly used application paths.
/// </summary>
public static class PathsHelper
{
    private static string? _base;
    public static string BaseDir => _base ??= AppDomain.CurrentDomain.BaseDirectory;

    private static string? _userData;
    public static string UserDataDir => _userData ??= Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MaaAssistantArknights");

    private static string? _resource;
    public static string ResourceDir => _resource ??= Path.Combine(BaseDir, "resource");

    private static string? _cache;
    public static string CacheDir => _cache ??= Path.Combine(UserDataDir, "cache");

    private static string? _config;
    public static string ConfigDir => _config ??= Path.Combine(UserDataDir, "config");

    private static string? _debug;
    public static string DebugDir => _debug ??= Path.Combine(UserDataDir, "debug");

    private static string? _data;
    public static string DataDir => _data ??= Path.Combine(UserDataDir, "data");
}
