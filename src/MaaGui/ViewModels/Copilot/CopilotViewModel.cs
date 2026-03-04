using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaGui.Services.MaaInterop;
using Serilog;

namespace MaaGui.ViewModels.Copilot;

public partial class CopilotViewModel : ObservableObject
{
    private readonly AsstProxy _asstProxy;

    [ObservableProperty]
    private string _filename = string.Empty;

    [ObservableProperty]
    private bool _running = false;

    public CopilotViewModel(AsstProxy asstProxy)
    {
        _asstProxy = asstProxy;
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        // For now, just a placeholder as file picker needs UI thread access
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (string.IsNullOrEmpty(Filename)) return;

        Running = true;
        Log.Information("Starting Copilot with file: {Filename}", Filename);

        var taskParams = $"{{\"filename\": \"{Filename.Replace("\\", "\\\\")}\"}}";
        _asstProxy.AppendTask("Copilot", taskParams);
        _asstProxy.Start();

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Stop()
    {
        _asstProxy.Stop();
        Running = false;
    }
}
