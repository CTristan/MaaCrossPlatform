using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Timers;
using MaaGui.Configuration.Tasks;
using Serilog;

namespace MaaGui.Configuration.Factory;

/// <summary>
/// Configuration factory for loading and saving configurations
/// </summary>
public static class ConfigFactory
{
    private static string _configDir = Helper.PathsHelper.ConfigDir;

    private static string _configFile = Path.Combine(_configDir, "gui.new.json");
    private static string _configBakFile = Path.Combine(_configDir, "gui.new.json.bak");

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly System.Timers.Timer _debounceTimer;
    private static Root? _currentConfig;
    private static readonly object _lock = new();
    private static int _pendingDelayMs = 200;

    static ConfigFactory()
    {
        _debounceTimer = new System.Timers.Timer(_pendingDelayMs)
        {
            AutoReset = false
        };
        _debounceTimer.Elapsed += (s, e) => SaveCallback(null);
    }

    /// <summary>
    /// Set the configuration directory (useful for tests)
    /// </summary>
    public static void SetConfigDir(string path)
    {
        lock (_lock)
        {
            _configDir = path;
            _configFile = Path.Combine(_configDir, "gui.new.json");
            _configBakFile = Path.Combine(_configDir, "gui.new.json.bak");
            _currentConfig = null;
        }
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public static Root Load()
    {
        lock (_lock)
        {
            EnsureDirectoryExists();

            Root? parsed = null;

            if (File.Exists(_configFile))
            {
                try
                {
                    var json = File.ReadAllText(_configFile);
                    parsed = JsonSerializer.Deserialize<Root>(json, _options);
                    Log.Information("Configuration loaded from {ConfigFile}", _configFile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load configuration file");
                    // Try backup
                    if (File.Exists(_configBakFile))
                    {
                        try
                        {
                            var backupJson = File.ReadAllText(_configBakFile);
                            parsed = JsonSerializer.Deserialize<Root>(backupJson, _options);
                            Log.Information("Configuration loaded from backup file");
                        }
                        catch (Exception backupEx)
                        {
                            Log.Error(backupEx, "Failed to load backup configuration");
                        }
                    }
                }
            }

            _currentConfig = parsed ?? CreateDefault();
            return _currentConfig;
        }
    }

    /// <summary>
    /// Save configuration to file with debouncing
    /// </summary>
    public static void Save()
    {
        lock (_lock)
        {
            if (_currentConfig == null)
            {
                Log.Warning("Cannot save: no configuration loaded");
                return;
            }

            // Debounce save operation - restart the timer
            _debounceTimer.Stop();
            _debounceTimer.Interval = _pendingDelayMs;
            _debounceTimer.Start();
        }
    }

    /// <summary>
    /// Force immediate save (without debouncing)
    /// </summary>
    public static void SaveImmediately()
    {
        lock (_lock)
        {
            if (_currentConfig == null)
            {
                Log.Warning("Cannot save: no configuration loaded");
                return;
            }

            try
            {
                EnsureDirectoryExists();

                var json = JsonSerializer.Serialize(_currentConfig, _options);
                var tempFile = _configFile + ".tmp";
                File.WriteAllText(tempFile, json);

                // Create backup before overwriting
                if (File.Exists(_configFile))
                {
                    File.Copy(_configFile, _configBakFile, true);
                }

                // Atomic replacement or move
                if (File.Exists(_configFile))
                {
                    File.Replace(tempFile, _configFile, null);
                }
                else
                {
                    File.Move(tempFile, _configFile);
                }

                Log.Debug("Configuration saved to {ConfigFile}", _configFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save configuration file");
            }
        }
    }

    /// <summary>
    /// Get current configuration
    /// </summary>
    public static Root CurrentConfig
    {
        get
        {
            if (_currentConfig == null)
            {
                lock (_lock)
                {
                    if (_currentConfig == null)
                    {
                        _currentConfig = Load();
                    }
                }
            }
            return _currentConfig;
        }
    }

    /// <summary>
    /// Get the config directory path
    /// </summary>
    public static string ConfigDir => _configDir;

    /// <summary>
    /// Dispose the debounce timer. Should be called when the application is shutting down.
    /// </summary>
    public static void Dispose()
    {
        lock (_lock)
        {
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
            }
        }
    }

    private static void SaveCallback(object? state)
    {
        SaveImmediately();
    }

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_configDir))
        {
            Directory.CreateDirectory(_configDir);
            Log.Information("Created configuration directory: {ConfigDir}", _configDir);
        }
    }

    private static Root CreateDefault()
    {
        var defaultConfig = new Root();

        var defaultConfigItem = new SpecificConfig { Name = "Default" };
        defaultConfigItem.Tasks.Add(new StartUpTask { Name = "Start Up" });
        defaultConfigItem.Tasks.Add(new FightTask { Name = "Fight" });
        defaultConfigItem.Tasks.Add(new InfrastTask { Name = "Infrast" });
        defaultConfigItem.Tasks.Add(new MallTask { Name = "Mall" });
        defaultConfigItem.Tasks.Add(new RecruitTask { Name = "Recruit" });
        defaultConfigItem.Tasks.Add(new AwardTask { Name = "Award" });
        defaultConfigItem.Tasks.Add(new RoguelikeTask { Name = "Roguelike", Enabled = false });
        defaultConfigItem.Tasks.Add(new ReclamationTask { Name = "Reclamation", Enabled = false });
        defaultConfigItem.Tasks.Add(new CopilotTask { Name = "Copilot", Enabled = false });

        defaultConfig.Configurations.Add(defaultConfigItem);
        defaultConfig.Current = "Default";

        Log.Information("Created default configuration with basic tasks");
        return defaultConfig;
    }
}
