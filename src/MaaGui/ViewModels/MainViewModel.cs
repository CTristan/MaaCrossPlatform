using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaGui.Models;
using MaaGui.Services.MaaInterop;
using MaaGui.ViewModels.Copilot;
using MaaGui.ViewModels.Utilities;
using MaaGui.ViewModels.Settings;

namespace MaaGui.ViewModels;

/// <summary>
/// Main application ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly TaskQueueViewModel _taskQueue;
    private readonly CopilotViewModel _copilot;
    private readonly UtilitiesViewModel _utilities;
    private readonly SettingsViewModel _settings;

    [ObservableProperty]
    private string _title = "MaaAssistantArknights";

    [ObservableProperty]
    private string _selectedTab = "DailyTasks";

    [ObservableProperty]
    private string _connectionStatus = "Not Connected";

    public TaskQueueViewModel TaskQueue => _taskQueue;
    public CopilotViewModel Copilot => _copilot;
    public UtilitiesViewModel Utilities => _utilities;
    public SettingsViewModel Settings => _settings;

    public MainViewModel(TaskQueueViewModel taskQueue, CopilotViewModel copilot, UtilitiesViewModel utilities, SettingsViewModel settings)
    {
        _taskQueue = taskQueue;
        _copilot = copilot;
        _utilities = utilities;
        _settings = settings;
    }

    /// <summary>
    /// Expand settings for a task item
    /// </summary>
    [RelayCommand]
    private void ExpandSettings(MaaGui.Models.TaskItemViewModel task)
    {
        if (task == null)
            return;

        task.IsExpanded = !task.IsExpanded;
    }
}
