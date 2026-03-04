using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using MaaGui.Constants;
using Serilog;

namespace MaaGui.Services.MaaInterop;

/// <summary>
/// Callback event arguments
/// </summary>
public class CallbackEventArgs : EventArgs
{
    public AsstMsg Message { get; }
    public JsonDocument Details { get; }

    public CallbackEventArgs(AsstMsg message, JsonDocument details)
    {
        Message = message;
        Details = details;
    }
}

/// <summary>
/// High-level MaaCore wrapper that manages handle lifecycle, callback marshalling, and task orchestration
/// </summary>
public class AsstProxy : IDisposable
{
    private static readonly ILogger _logger = Log.ForContext<AsstProxy>();
    private readonly IMaaService _maaService;
    private nint _handle = 0;
    private readonly AsstApiCallback _callback;
    private bool _disposed = false;
    private readonly ConcurrentDictionary<int, (TaskType Type, Constants.TaskStatus Status)> _tasksStatus = new();
    private TaskCompletionSource<bool>? _connectTcs;
    private readonly object _connectLock = new object();

    public event EventHandler<CallbackEventArgs>? CallbackReceived;

    public AsstProxy(IMaaService maaService)
    {
        _maaService = maaService;
        _callback = CallbackFunction;
    }

    /// <summary>
    /// Initialize MaaCore instance
    /// </summary>
    public bool Initialize(string baseDir, string userDir)
    {
        _logger.Information("Initializing MaaCore with base directory: {BaseDir} and user directory: {UserDir}", baseDir, userDir);

        // Ensure user directory exists
        if (!System.IO.Directory.Exists(userDir))
        {
            System.IO.Directory.CreateDirectory(userDir);
        }

        // Set user directory
        if (!_maaService.SetUserDir(userDir))
        {
            _logger.Error("Failed to set user directory");
            return false;
        }

        // Load resources
        if (!_maaService.LoadResource(baseDir))
        {
            _logger.Error("Failed to load resources");
            return false;
        }

        // Create instance with callback
        _handle = _maaService.CreateEx(_callback, nint.Zero);

        if (_handle == 0)
        {
            _logger.Error("Failed to create MaaCore instance");
            return false;
        }

        _logger.Information("MaaCore initialized successfully, handle: {Handle}", _handle);
        return true;
    }

    /// <summary>
    /// Connect to a device
    /// </summary>
    public async Task<bool> ConnectAsync(string adbPath, string address, string config)
    {
        lock (_connectLock)
        {
            if (_connectTcs != null && !_connectTcs.Task.IsCompleted)
            {
                _logger.Warning("Connection already in progress, ignoring new connection request to {Address}", address);
                return false;
            }

            _logger.Information("Connecting to device: {Address}", address);
            _connectTcs = new TaskCompletionSource<bool>();
        }

        var callId = _maaService.AsyncConnect(_handle, adbPath, address, config, true);

        // Wait for connection result (handled via callbacks) with a 30-second timeout
        var completedTask = await Task.WhenAny(_connectTcs.Task, Task.Delay(30000));
        if (completedTask == _connectTcs.Task)
        {
            return await _connectTcs.Task;
        }

        _logger.Warning("Connection timeout for {Address}", address);
        lock (_connectLock)
        {
            Connected = false;
        }
        return false;
    }

    /// <summary>
    /// Append a task to the task queue
    /// </summary>
    public int AppendTask(string type, string @params)
    {
        var taskId = _maaService.AppendTask(_handle, type, @params);
        _logger.Debug("Appended task: Type={Type}, TaskId={TaskId}", type, taskId);

        if (taskId > 0 && Enum.TryParse<TaskType>(type, out var taskType))
        {
            _tasksStatus[taskId] = (taskType, Constants.TaskStatus.Pending);
        }

        return taskId;
    }

    /// <summary>
    /// Load resource when idle
    /// </summary>
    public async Task LoadResourceWhenIdleAsync()
    {
        // For now, just a stub
        await Task.Yield();
        Log.Debug("LoadResourceWhenIdleAsync called");
    }

    /// <summary>
    /// Start task execution
    /// </summary>
    public bool Start()
    {
        _logger.Information("Starting task execution");
        return _maaService.Start(_handle);
    }

    /// <summary>
    /// Stop task execution
    /// </summary>
    public bool Stop()
    {
        _logger.Information("Stopping task execution");
        return _maaService.Stop(_handle);
    }

    /// <summary>
    /// Check if tasks are running
    /// </summary>
    public bool Running => _maaService.Running(_handle);

    /// <summary>
    /// Check if connected to a device
    /// </summary>
    public bool Connected { get; private set; }

    /// <summary>
    /// Get task status
    /// </summary>
    public (TaskType Type, Constants.TaskStatus Status)? GetTaskStatus(int taskId)
    {
        return _tasksStatus.TryGetValue(taskId, out var status) ? status : null;
    }

    /// <summary>
    /// Get all task statuses
    /// </summary>
    public IReadOnlyDictionary<int, (TaskType Type, Constants.TaskStatus Status)> GetTaskStatuses()
    {
        return _tasksStatus;
    }

    /// <summary>
    /// Go back to home screen
    /// </summary>
    public bool BackToHome() => _maaService.BackToHome(_handle);

    /// <summary>
    /// Get captured image
    /// </summary>
    public byte[]? GetImage()
    {
        const int bufferSize = 1280 * 720 * 3;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var size = _maaService.GetImage(_handle, buffer, bufferSize);
            if (size == _maaService.GetNullSize())
            {
                return null;
            }
            return buffer.Take(size).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Get device UUID
    /// </summary>
    public string GetUUID() => _maaService.GetUUID(_handle);

    /// <summary>
    /// Get MaaCore version
    /// </summary>
    public string GetVersion() => _maaService.GetVersion();

    /// <summary>
    /// Set instance option
    /// </summary>
    public bool SetInstanceOption(AsstInstanceOptionKey key, string value)
    {
        return _maaService.SetInstanceOption(_handle, key, value);
    }

    /// <summary>
    /// Set static option
    /// </summary>
    public bool SetStaticOption(AsstStaticOptionKey key, string value)
    {
        return _maaService.SetStaticOption(key, value);
    }

    /// <summary>
    /// Set connection extras
    /// </summary>
    public void SetConnectionExtras(string name, string extras)
    {
        _maaService.SetConnectionExtras(name, extras);
    }

#if WINDOWS
    /// <summary>
    /// Attach to a Win32 window
    /// </summary>
    public async Task<bool> AttachWindowAsync(nint hwnd, ulong screencapMethod, ulong mouseMethod, ulong keyboardMethod)
    {
        _logger.Information("Attaching to window: {Hwnd}", hwnd);
        var callId = _maaService.AsyncAttachWindow(_handle, hwnd, screencapMethod, mouseMethod, keyboardMethod, true);
        await Task.Delay(100);
        lock (_connectLock)
        {
            return Connected;
        }
    }
#endif

    private void CallbackFunction(int msg, string detailsJson, nint customArg)
    {
        try
        {
            using var json = JsonDocument.Parse(detailsJson ?? "{}");
            var callbackArgs = new CallbackEventArgs((AsstMsg)msg, json);

            // Process callback
            ProcessCallback((AsstMsg)msg, json);

            // Raise event
            CallbackReceived?.Invoke(this, callbackArgs);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing callback");
        }
    }

    private void ProcessCallback(AsstMsg msg, JsonDocument details)
    {
        switch (msg)
        {
            case AsstMsg.InitFailed:
                _logger.Error("MaaCore initialization failed");
                break;

            case AsstMsg.ConnectionInfo:
                ProcessConnectionInfo(details);
                break;

            case AsstMsg.TaskChainStart:
                ProcessTaskChainStart(details);
                break;

            case AsstMsg.TaskChainCompleted:
                ProcessTaskChainCompleted(details);
                break;

            case AsstMsg.TaskChainError:
                ProcessTaskChainError(details);
                break;

            case AsstMsg.TaskChainStopped:
                ProcessTaskChainStopped(details);
                break;

            case AsstMsg.AllTasksCompleted:
                ProcessAllTasksCompleted(details);
                break;

            case AsstMsg.SubTaskError:
                ProcessSubTaskError(details);
                break;

            case AsstMsg.SubTaskStart:
                ProcessSubTaskStart(details);
                break;

            case AsstMsg.SubTaskCompleted:
                ProcessSubTaskCompleted(details);
                break;

            case AsstMsg.Destroyed:
                _logger.Information("MaaCore instance destroyed");
                break;
        }
    }

    private void ProcessConnectionInfo(JsonDocument details)
    {
        var what = details.RootElement.GetProperty("what").GetString();
        switch (what)
        {
            case "Connected":
                lock (_connectLock)
                {
                    Connected = true;
                }
                _connectTcs?.TrySetResult(true);
                var adb = details.RootElement.GetProperty("details").GetProperty("adb").GetString();
                var address = details.RootElement.GetProperty("details").GetProperty("address").GetString();
                _logger.Information("Connected to device: {Address}", address);
                break;

            case "UnsupportedResolution":
            case "ResolutionError":
                lock (_connectLock)
                {
                    Connected = false;
                }
                _connectTcs?.TrySetResult(false);
                _logger.Error("Resolution error");
                break;

            case "Disconnect":
                lock (_connectLock)
                {
                    Connected = false;
                }
                _connectTcs?.TrySetResult(false);
                _logger.Warning("Disconnected from device");
                break;
        }
    }

    private void ProcessTaskChainStart(JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        var taskId = details.RootElement.GetProperty("taskid").GetInt32();
        _logger.Information("Task chain started: {TaskChain}, TaskId: {TaskId}", taskChain, taskId);

        if (_tasksStatus.TryGetValue(taskId, out var status))
        {
            _tasksStatus[taskId] = (status.Type, Constants.TaskStatus.InProgress);
        }
    }

    private void ProcessTaskChainCompleted(JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        var taskId = details.RootElement.GetProperty("taskid").GetInt32();
        _logger.Information("Task chain completed: {TaskChain}, TaskId: {TaskId}", taskChain, taskId);

        if (_tasksStatus.TryGetValue(taskId, out var status))
        {
            _tasksStatus[taskId] = (status.Type, Constants.TaskStatus.Completed);
        }
    }

    private void ProcessTaskChainError(JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        var taskId = details.RootElement.GetProperty("taskid").GetInt32();
        _logger.Error("Task chain error: {TaskChain}, TaskId: {TaskId}", taskChain, taskId);

        if (_tasksStatus.TryGetValue(taskId, out var status))
        {
            _tasksStatus[taskId] = (status.Type, Constants.TaskStatus.Error);
        }
    }

    private void ProcessTaskChainStopped(JsonDocument details)
    {
        var taskChain = details.RootElement.GetProperty("taskchain").GetString();
        var taskId = details.RootElement.GetProperty("taskid").GetInt32();
        _logger.Information("Task chain stopped: {TaskChain}, TaskId: {TaskId}", taskChain, taskId);

        _tasksStatus.Clear();
    }

    private void ProcessAllTasksCompleted(JsonDocument details)
    {
        _logger.Information("All tasks completed");
        _tasksStatus.Clear();
    }

    private void ProcessSubTaskError(JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        _logger.Error("Sub-task error: {SubTask}", subTask);
    }

    private void ProcessSubTaskStart(JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        _logger.Debug("Sub-task started: {SubTask}", subTask);
    }

    private void ProcessSubTaskCompleted(JsonDocument details)
    {
        var subTask = details.RootElement.GetProperty("subtask").GetString();
        _logger.Debug("Sub-task completed: {SubTask}", subTask);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != 0)
            {
                _maaService.Destroy(_handle);
                _handle = 0;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~AsstProxy()
    {
        Dispose();
    }
}
