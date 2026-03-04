using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MaaGui.Localization;

namespace MaaGui.Models;

/// <summary>
/// Generic combined data class.
/// </summary>
public partial class CombinedData : ObservableObject
{
    [ObservableProperty]
    private string _display = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public override string ToString() => Display;
}

/// <summary>
/// Stage activity info
/// </summary>
public class StageActivityInfo
{
    public string? Tip { get; set; }
    public string? StageName { get; set; }
    public DateTime UtcExpireTime { get; set; } = DateTime.MaxValue;
    public DateTime UtcStartTime { get; set; } = DateTime.MinValue;
    public bool IsResourceCollection { get; set; } = false;

    public bool BeingOpen => !NotOpenYet && !IsExpired;
    public bool IsExpired => DateTime.UtcNow >= UtcExpireTime;
    public bool NotOpenYet => DateTime.UtcNow <= UtcStartTime;
}

/// <summary>
/// Stage info
/// </summary>
public partial class StageInfo : CombinedData
{
    public string Tip { get; set; } = string.Empty;
    public IEnumerable<DayOfWeek>? OpenDaysOfWeek { get; set; }
    public StageActivityInfo? Activity { get; set; }
    public bool IsHidden { get; set; }
    public string? Drop { get; set; }
    public List<List<string>>? DropGroups { get; set; }

    public StageInfo() { }

    public StageInfo(string name, string tipKey, IEnumerable<DayOfWeek> openDaysOfWeek, StageActivityInfo activity, List<List<string>>? dropGroups = null)
    {
        Value = name;
        Display = LocalizationHelper.GetString(name);
        OpenDaysOfWeek = openDaysOfWeek;
        Activity = activity;
        DropGroups = dropGroups;

        if (!string.IsNullOrEmpty(tipKey))
        {
            Tip = LocalizationHelper.GetString(tipKey);
        }
    }

    public StageInfo(string display, string value, string? drop, StageActivityInfo activity)
    {
        Display = display;
        Value = value;
        Drop = drop;
        Activity = activity;
    }

    public bool IsActivityClosed()
    {
        return Activity is { BeingOpen: false, IsResourceCollection: false };
    }

    public bool IsStageOpen(DayOfWeek dayOfWeek)
    {
        if (Activity != null)
        {
            if (Activity.BeingOpen)
            {
                return true;
            }

            if (!Activity.IsResourceCollection)
            {
                return false;
            }
        }

        if (OpenDaysOfWeek != null && OpenDaysOfWeek.Any())
        {
            return OpenDaysOfWeek.Contains(dayOfWeek);
        }

        return true;
    }

    public bool IsStageOpenOrWillOpen()
    {
        if (Activity == null)
        {
            return true;
        }

        return !Activity.IsExpired || Activity.IsResourceCollection;
    }
}

/// <summary>
/// Mini-game entry
/// </summary>
public class MiniGameEntry
{
    public string Display { get; set; } = string.Empty;
    public string? DisplayKey { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Tip { get; set; }
    public string? TipKey { get; set; }
    public string? MinimumRequired { get; set; }
    public DateTime UtcStartTime { get; set; } = DateTime.MinValue;
    public DateTime UtcExpireTime { get; set; } = DateTime.MaxValue;

    public bool BeingOpen => !NotOpenYet && !IsExpired;
    public bool IsExpired => DateTime.UtcNow >= UtcExpireTime;
    public bool NotOpenYet => DateTime.UtcNow <= UtcStartTime;
}
