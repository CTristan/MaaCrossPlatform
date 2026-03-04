using CommunityToolkit.Mvvm.ComponentModel;
using MaaGui.Configuration.Tasks;

namespace MaaGui.Models;

/// <summary>
/// Task item status
/// </summary>
public enum TaskItemStatus
{
    Idle = 0,
    Pending,
    Running,
    Completed,
    Error
}

/// <summary>
/// Base task item model
/// </summary>
public partial class TaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Constants.TaskType _taskTypeValue = Constants.TaskType.Unknown;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private TaskItemStatus _status = TaskItemStatus.Idle;

    [ObservableProperty]
    private int _index = 0;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private BaseTask? _taskConfig;

    partial void OnIsEnabledChanged(bool value)
    {
        if (TaskConfig != null)
        {
            TaskConfig.Enabled = value;
        }
    }
}
