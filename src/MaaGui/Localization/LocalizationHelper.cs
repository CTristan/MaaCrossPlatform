using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Serilog;

namespace MaaGui.Localization;

/// <summary>
/// Localization helper for Avalonia GUI
/// </summary>
public class LocalizationHelper
{
    private static readonly Dictionary<string, string> _supportedLanguages = new()
    {
        { "zh-cn", "简体中文" },
        { "zh-tw", "繁體中文" },
        { "en-us", "English" },
        { "ja-jp", "日本語" },
        { "ko-kr", "한국어" },
        { "pallas", "🍻🍻🍻" },
    };

    private static readonly string[] _fallbackChain = ["zh-cn", "en-us"];
    private static readonly Dictionary<string, string> _currentStrings = new();
    private static string _currentLanguage = "en-us";

    /// <summary>
    /// Get supported languages
    /// </summary>
    public static IReadOnlyDictionary<string, string> SupportedLanguages => _supportedLanguages;

    /// <summary>
    /// Get current language
    /// </summary>
    public static string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Get default language based on system locale
    /// </summary>
    public static string GetDefaultLanguage()
    {
        var systemLang = CultureInfo.CurrentCulture.Name.ToLower();

        // Exact match
        if (_supportedLanguages.ContainsKey(systemLang))
        {
            return systemLang;
        }

        // Language-only match (e.g., "zh" for "zh-cn")
        var langPrefix = systemLang.Split('-')[0];
        var matched = _supportedLanguages.Keys.FirstOrDefault(k => k.StartsWith(langPrefix));
        if (matched != null)
        {
            return matched;
        }

        return "en-us";
    }

    /// <summary>
    /// Load localization for the specified language
    /// </summary>
    public static void Load(string language)
    {
        _currentLanguage = language;
        _currentStrings.Clear();

        // Determine fallback chain
        var cultureList = GetCultureList(language);

        foreach (var culture in cultureList)
        {
            try
            {
                var resourceDict = LoadResourceDictionary(culture);
                if (resourceDict != null)
                {
                    MergeResourceDictionary(resourceDict, _currentStrings);
                    Log.Debug("Loaded localization for {Culture}", culture);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load localization for {Culture}", culture);
            }
        }

        Log.Information("Loaded localization: {Language}, {Count} strings", language, _currentStrings.Count);
    }

    /// <summary>
    /// Get localized string for the specified key
    /// </summary>
    public static string GetString(string key)
    {
        if (_currentStrings.TryGetValue(key, out var value))
        {
            return value;
        }

        Log.Debug("Missing localization key: {Key}", key);
        return $"[{key}]";
    }

    /// <summary>
    /// Get formatted localized string
    /// </summary>
    public static string GetStringFormat(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(format, args);
    }

    /// <summary>
    /// Get string using nested key syntax (e.g., "TaskQueue.Start")
    /// </summary>
    public static string GetStringNested(string nestedKey)
    {
        var parts = nestedKey.Split('.');
        var baseKey = parts[0];
        var subKey = parts.Length > 1 ? parts[1] : string.Empty;

        if (string.IsNullOrEmpty(subKey))
        {
            return GetString(baseKey);
        }

        // Try composite key first
        var compositeKey = $"{baseKey}.{subKey}";
        if (_currentStrings.TryGetValue(compositeKey, out var value))
        {
            return value;
        }

        // Fallback to base key
        return GetString(baseKey);
    }

    /// <summary>
    /// Check if pallas mode is active
    /// </summary>
    public static bool IsPallasMode => _currentLanguage == "pallas";

    /// <summary>
    /// Get culture list for the specified language with fallback chain
    /// </summary>
    private static List<string> GetCultureList(string language)
    {
        var list = new List<string>();

        switch (language)
        {
            case "zh-cn":
                list.Add("zh-cn");
                break;

            case "zh-tw":
                list.AddRange(["zh-cn", "zh-tw"]);
                break;

            case "en-us":
                list.AddRange(["zh-cn", "en-us"]);
                break;

            case "ja-jp":
                list.AddRange(["zh-cn", "en-us", "ja-jp"]);
                break;

            case "ko-kr":
                list.AddRange(["zh-cn", "en-us", "ko-kr"]);
                break;

            case "pallas":
                // Pallas uses zh-cn strings but with modification
                list.Add("zh-cn");
                break;

            default:
                list.AddRange(_fallbackChain);
                break;
        }

        return list;
    }

    /// <summary>
    /// Load resource dictionary from .axaml file
    /// </summary>
    private static ResourceDictionary? LoadResourceDictionary(string culture)
    {
        var uri = new Uri($"avares://MaaGui/Localization/Strings/{culture}.axaml");
        try
        {
            var xaml = AvaloniaXamlLoader.Load(uri);
            if (xaml is ResourceDictionary dict)
            {
                return dict;
            }
        }
        catch
        {
            // File doesn't exist
        }

        return null;
    }

    /// <summary>
    /// Merge resource dictionary into strings dictionary
    /// </summary>
    private static void MergeResourceDictionary(ResourceDictionary resourceDict, Dictionary<string, string> targetDict)
    {
        foreach (var key in resourceDict.Keys)
        {
            var keyValue = resourceDict[key];
            if (keyValue is string value)
            {
                // Skip if already exists (first loaded wins for fallback chain)
                var keyStr = key.ToString();
                if (!string.IsNullOrEmpty(keyStr))
                {
                    targetDict.TryAdd(keyStr, value);
                }
            }
        }
    }
}
