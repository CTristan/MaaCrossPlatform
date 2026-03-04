using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaGui.Configuration;
using MaaGui.Configuration.Tasks;
using MaaGui.Models;
using MaaGui.Platform;
using MaaGui.Services;
using MaaGui.Services.MaaInterop;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Task = System.Threading.Tasks.Task;

namespace MaaGui.ViewModels;

/// <summary>
/// Task queue orchestration ViewModel
/// </summary>
public partial class TaskQueueViewModel : ObservableObject, IDisposable
{
    private readonly AsstProxy _asstProxy;
    private readonly StageManager _stageManager;
    private readonly Root _config;
    private readonly LogEntryCollection _logEntries = new();
    private readonly System.Timers.Timer _checkTimer = new();
    private DateTime _lastCheckTime = DateTime.MinValue;
    private bool _disposed = false;

    [ObservableProperty]
    private string _stageTips = string.Empty;

    [ObservableProperty]
    private bool _running = false;

    [ObservableProperty]
    private bool _connected = false;

    [ObservableProperty]
    private string _connectionStatus = "Not Connected";

    public ObservableCollection<TaskItemViewModel> TaskItems { get; } = new();
    public ObservableCollection<LogEntry> LogEntries => _logEntries;

    /// <summary>
    /// Constructor
    /// </summary>
    public TaskQueueViewModel(AsstProxy asstProxy, StageManager stageManager, Root config)
    {
        _asstProxy = asstProxy;
        _stageManager = stageManager;
        _config = config;

        // Subscribe to callback events
        _asstProxy.CallbackReceived += OnMaaCallback;

        // Initialize default tasks
        InitializeDefaultTasks();

        // Load stage tips
        UpdateStageTips();

        // Initialize timer
        InitTimer();
    }

    private void InitTimer()
    {
        _checkTimer.Interval = 30 * 1000; // Check every 30 seconds
        _checkTimer.AutoReset = true;
        _checkTimer.Elapsed += (s, e) => HandleTimerCheck();
        _checkTimer.Start();
    }

    private void HandleTimerCheck()
    {
        var now = DateTime.Now;
        // Normalize to minute
        var currentMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        if (currentMinute <= _lastCheckTime) return;
        _lastCheckTime = currentMinute;

        // Check each enabled timer
        foreach (var timer in _config.Timers)
        {
            if (!timer.Enabled || string.IsNullOrEmpty(timer.Time)) continue;

            if (TimeSpan.TryParse(timer.Time, out var scheduledTime))
            {
                if (currentMinute.TimeOfDay == scheduledTime)
                {
                    Log.Information("Scheduled start triggered for time: {Time}", timer.Time);
                    InvokeStartAsync();
                }
            }
        }
    }

    private async void InvokeStartAsync()
    {
        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await StartAsync());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during scheduled start");
        }
    }

    private void UpdateStageTips()
    {
        StageTips = _stageManager.GetStageTips(DateTime.Now.DayOfWeek);
    }

    /// <summary>
    /// Initialize default task list
    /// </summary>
    private void InitializeDefaultTasks()
    {
        TaskItems.Clear();

        var currentConfigName = _config.Current;
        var currentConfig = _config.Configurations.FirstOrDefault(c => c.Name == currentConfigName);

        if (currentConfig == null)
        {
            Log.Warning("Current configuration {ConfigName} not found, falling back to first or creating default", currentConfigName);
            currentConfig = _config.Configurations.FirstOrDefault();
            if (currentConfig == null)
            {
                // Should not happen with new CreateDefault
                return;
            }
        }

        int index = 0;
        foreach (var task in currentConfig.Tasks)
        {
            TaskItems.Add(new TaskItemViewModel
            {
                Name = task.Name,
                TaskTypeValue = task.TaskType,
                IsEnabled = task.Enabled,
                Status = TaskItemStatus.Idle,
                Index = index++,
                TaskConfig = task
            });
        }

        Log.Information("Initialized {Count} tasks from config {ConfigName}", TaskItems.Count, currentConfigName);
    }

    /// <summary>
    /// Connect to device
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (Connected) return;

        AddLog($"Connecting to {_config.GameSettings.Address}...", LogLevel.Info);

        var connected = await _asstProxy.ConnectAsync(_config.GameSettings.AdbPath, _config.GameSettings.Address, "Default");

        if (connected)
        {
            AddLog("Connection request sent", LogLevel.Debug);
        }
        else
        {
            AddLog("Failed to send connection request", LogLevel.Error);
        }
    }

    /// <summary>
    /// Start all enabled tasks
    /// </summary>
    [RelayCommand]
    private async Task StartAsync()
    {
        if (Running)
        {
            Log.Warning("Task queue is already running");
            return;
        }

        if (!Connected)
        {
            var connected = await _asstProxy.ConnectAsync(_config.GameSettings.AdbPath, _config.GameSettings.Address, "Default");
            if (!connected || !Connected)
            {
                AddLog("Failed to connect, cannot start tasks", LogLevel.Warning);
                return;
            }
        }

        var enabledTasks = TaskItems.Where(t => t.IsEnabled).ToList();
        if (enabledTasks.Count == 0)
        {
            AddLog("No tasks enabled", LogLevel.Warning);
            return;
        }

        Running = true;
        AddLog($"Starting {enabledTasks.Count} tasks", LogLevel.Info);

        // Prevent sleep during execution
        try
        {
            var platformServices = App.ServicesProvider.GetRequiredService<IPlatformServices>();
            platformServices.PreventSleep();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to prevent sleep");
        }

        // Append all enabled tasks to MaaCore
        foreach (var task in enabledTasks)
        {
            task.Status = TaskItemStatus.Pending;

            string taskParams = "{}";
            if (task.TaskConfig != null)
            {
                try
                {
                    taskParams = JsonSerializer.Serialize((object)task.TaskConfig, task.TaskConfig.GetType());
                    Log.Debug("Serialized task {TaskName} parameters: {Params}", task.Name, taskParams);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to serialize task {TaskName} parameters", task.Name);
                }
            }

            var taskId = _asstProxy.AppendTask(task.TaskTypeValue.ToString(), taskParams);

            if (taskId <= 0)
            {
                AddLog($"Failed to add {task.Name} task", LogLevel.Error);
                task.Status = TaskItemStatus.Error;
            }
            else
            {
                AddLog($"Added {task.Name} task (ID: {taskId})", LogLevel.Debug);
            }
        }

        // Start task execution
        var started = _asstProxy.Start();
        if (started)
        {
            AddLog("Task execution started", LogLevel.Info);
        }
        else
        {
            AddLog("Failed to start task execution", LogLevel.Error);
            Running = false;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop task execution
    /// </summary>
    [RelayCommand]
    private async Task StopAsync()
    {
        if (!Running)
        {
            return;
        }

        AddLog("Stopping tasks...", LogLevel.Info);

        var stopped = _asstProxy.Stop();

        // Restore sleep
        try
        {
            var platformServices = App.ServicesProvider.GetRequiredService<MaaGui.Platform.IPlatformServices>();
            platformServices.AllowSleep();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to restore sleep");
        }

        // Reset all task statuses
        foreach (var task in TaskItems)
        {
            task.Status = TaskItemStatus.Idle;
        }

        Running = false;

        AddLog(stopped ? "Tasks stopped" : "Failed to stop tasks", LogLevel.Info);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Toggle task enabled status
    /// </summary>
    [RelayCommand]
    private void ToggleTask(TaskItemViewModel task)
    {
        if (task == null)
            return;

        task.IsEnabled = !task.IsEnabled;
        AddLog($"{task.Name} {(task.IsEnabled ? "enabled" : "disabled")}", LogLevel.Debug);
    }

    /// <summary>
    /// Select all tasks
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var task in TaskItems)
        {
            task.IsEnabled = true;
        }

        AddLog("All tasks enabled", LogLevel.Debug);
    }

    /// <summary>
    /// Deselect all tasks
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var task in TaskItems)
        {
            task.IsEnabled = false;
        }

        AddLog("All tasks disabled", LogLevel.Debug);
    }

    /// <summary>
    /// Add log entry
    /// </summary>
    private void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = message,
            Level = level
        };

        _logEntries.Add(entry);

        var serilogLevel = level switch
        {
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Info => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            _ => Serilog.Events.LogEventLevel.Information
        };
        Log.Write(serilogLevel, message);

        // Keep log size manageable
        while (_logEntries.Count > 500)
        {
            _logEntries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Handle MaaCore callbacks
    /// </summary>
    private void OnMaaCallback(object? sender, CallbackEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            switch (e.Message)
            {
                case Constants.AsstMsg.ConnectionInfo:
                    HandleConnectionInfo(e.Details);
                    break;

                case Constants.AsstMsg.TaskChainStart:
                    HandleTaskChainStart(e.Details);
                    break;

                case Constants.AsstMsg.TaskChainCompleted:
                    HandleTaskChainCompleted(e.Details);
                    break;

                case Constants.AsstMsg.TaskChainError:
                    HandleTaskChainError(e.Details);
                    break;

                case Constants.AsstMsg.AllTasksCompleted:
                    HandleAllTasksCompleted(e.Details);
                    break;

                case Constants.AsstMsg.SubTaskStart:
                    HandleSubTaskStart(e.Details);
                    break;

                case Constants.AsstMsg.SubTaskCompleted:
                    HandleSubTaskCompleted(e.Details);
                    break;

                case Constants.AsstMsg.SubTaskError:
                    HandleSubTaskError(e.Details);
                    break;
            }
        });
    }

    private void HandleConnectionInfo(System.Text.Json.JsonDocument details)
    {
        var what = details.RootElement.GetProperty("what").GetString();

        switch (what)
        {
            case "Connected":
                Connected = true;
                ConnectionStatus = "Connected";
                AddLog("Connected to device", LogLevel.Info);
                break;

            case "Disconnect":
                Connected = false;
                ConnectionStatus = "Disconnected";
                AddLog("Disconnected from device", LogLevel.Warning);
                break;

            default:
                AddLog($"Connection info: {what}", LogLevel.Debug);
                break;
        }
    }

    private void HandleTaskChainStart(System.Text.Json.JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        if (Enum.TryParse<Constants.TaskType>(taskChain, true, out var taskType))
        {
            var task = TaskItems.FirstOrDefault(t => t.TaskTypeValue == taskType);

            if (task != null)
            {
                task.Status = TaskItemStatus.Running;
                AddLog($"Started: {task.Name}", LogLevel.Info);
            }
        }
    }

    private void HandleTaskChainCompleted(System.Text.Json.JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        if (Enum.TryParse<Constants.TaskType>(taskChain, true, out var taskType))
        {
            var task = TaskItems.FirstOrDefault(t => t.TaskTypeValue == taskType);

            if (task != null)
            {
                task.Status = TaskItemStatus.Completed;
                AddLog($"Completed: {task.Name}", LogLevel.Info);
            }
        }
    }

    private void HandleTaskChainError(System.Text.Json.JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        if (Enum.TryParse<Constants.TaskType>(taskChain, true, out var taskType))
        {
            var task = TaskItems.FirstOrDefault(t => t.TaskTypeValue == taskType);

            if (task != null)
            {
                task.Status = TaskItemStatus.Error;
                AddLog($"Error in: {task.Name}", LogLevel.Error);
            }
        }
    }

    private void HandleAllTasksCompleted(System.Text.Json.JsonDocument details)
    {
        Running = false;

        // Restore sleep
        try
        {
            var platformServices = App.ServicesProvider.GetRequiredService<MaaGui.Platform.IPlatformServices>();
            platformServices.AllowSleep();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to restore sleep");
        }

        // Reset all task statuses
        foreach (var task in TaskItems)
        {
            task.Status = TaskItemStatus.Idle;
        }

        AddLog("All tasks completed", LogLevel.Info);
    }

    private void HandleSubTaskStart(System.Text.Json.JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        AddLog($"Sub-task started: {subTask}", LogLevel.Debug);
    }

    private void HandleSubTaskCompleted(System.Text.Json.JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        AddLog($"Sub-task completed: {subTask}", LogLevel.Debug);
    }

    private void HandleSubTaskError(System.Text.Json.JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        AddLog($"Sub-task error: {subTask}", LogLevel.Error);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _checkTimer?.Stop();
            _checkTimer?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Log entry model
/// </summary>
public class LogEntry
{
    public string Time { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
}

/// <summary>
/// Log entry collection for thread-safe updates
/// </summary>
public class LogEntryCollection : ObservableCollection<LogEntry>
{
    public new void Add(LogEntry entry)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => base.Add(entry));
        }
        else
        {
            base.Add(entry);
        }
    }
}

/// <summary>
/// Log level
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
