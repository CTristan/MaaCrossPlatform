using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaGui.Services.MaaInterop;
using Serilog;

namespace MaaGui.ViewModels.Utilities;

public partial class UtilitiesViewModel : ObservableObject
{
    private readonly AsstProxy _asstProxy;

    public ObservableCollection<UtilityItem> Utilities { get; } = new();

    public UtilitiesViewModel(AsstProxy asstProxy)
    {
        _asstProxy = asstProxy;
        InitializeUtilities();
    }

    private void InitializeUtilities()
    {
        Utilities.Add(new UtilityItem("Recruit", "Recruitment calculator and automation", "Recruit"));
        Utilities.Add(new UtilityItem("Depot", "Depot analysis", "Depot"));
        Utilities.Add(new UtilityItem("OperBox", "Operator box statistics", "OperBox"));
        Utilities.Add(new UtilityItem("Video", "Video recognition", "Video"));
        Utilities.Add(new UtilityItem("Gacha", "Gacha statistics", "Gacha"));
        Utilities.Add(new UtilityItem("MiniGame", "Mini-game automation", "MiniGame"));
    }

    [RelayCommand]
    private void RunUtility(UtilityItem item)
    {
        if (item == null) return;
        Log.Information("Running utility: {UtilityName}", item.Name);
        _asstProxy.AppendTask(item.TaskName, "{}");
        _asstProxy.Start();
    }
}

public class UtilityItem
{
    public string Name { get; }
    public string Description { get; }
    public string TaskName { get; }

    public UtilityItem(string name, string description, string taskName)
    {
        Name = name;
        Description = description;
        TaskName = taskName;
    }
}
