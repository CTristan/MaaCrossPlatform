using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using MaaGui.Configuration.Tasks;

namespace MaaGui.Configuration;

/// <summary>
/// Root configuration class
/// </summary>
public class Root : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonInclude]
    public int ConfigVersion { get; set; } = 1;

    [JsonInclude]
    public string Current { get; set; } = "Default";

    [JsonInclude]
    public ObservableCollection<SpecificConfig> Configurations { get; set; } = new();

    [JsonInclude]
    public ObservableCollection<TimerConfig> Timers { get; set; } = new();

    [JsonInclude]
    public VersionUpdate VersionUpdate { get; set; } = new();

    [JsonInclude]
    public AnnouncementInfo AnnouncementInfo { get; set; } = new();

    [JsonInclude]
    public GUI GUI { get; set; } = new();

    [JsonInclude]
    public GameSettings GameSettings { get; set; } = new();
}

/// <summary>
/// Version update configuration
/// </summary>
public class VersionUpdate
{
    [JsonPropertyName("auto_check")]
    public bool AutoCheck { get; set; } = true;

    [JsonPropertyName("update_channel")]
    public string UpdateChannel { get; set; } = "Stable";
}

/// <summary>
/// Announcement info configuration
/// </summary>
public class AnnouncementInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// GUI configuration
/// </summary>
public class GUI
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-us";

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Default";

    [JsonPropertyName("scale_factor")]
    public double ScaleFactor { get; set; } = 1.0;
}

/// <summary>
/// Game settings configuration
/// </summary>
public class GameSettings
{
    [JsonPropertyName("client_type")]
    public string ClientType { get; set; } = "Official";

    [JsonPropertyName("adb_path")]
    public string AdbPath { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = "127.0.0.1:5555";
}

/// <summary>
/// Specific task configuration
/// </summary>
public class SpecificConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public ObservableCollection<BaseTask> Tasks { get; set; } = new();
}

/// <summary>
/// Timer configuration
/// </summary>
public class TimerConfig
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = "00:00";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
