using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaGui.Configuration;
using MaaGui.Configuration.Factory;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MaaGui.ViewModels.Settings;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Root _config;

    [ObservableProperty]
    private string _adbPath = string.Empty;

    [ObservableProperty]
    private string _address = "127.0.0.1:5555";

    [ObservableProperty]
    private string _clientType = "Official";

    [ObservableProperty]
    private string _language = "en-us";

    public ObservableCollection<string> ClientTypes { get; } = new() { "Official", "Bilibili", "YoStarEN", "YoStarJP", "YoStarKR" };
    public ObservableCollection<string> Languages { get; } = new() { "en-us", "zh-cn" };

    public SettingsViewModel(Root config)
    {
        _config = config;

        // Initialize from config
        AdbPath = _config.GameSettings.AdbPath;
        Address = _config.GameSettings.Address;
        ClientType = _config.GameSettings.ClientType;
        Language = _config.GUI.Language;
    }

    [RelayCommand]
    private async Task UpdateDataAsync()
    {
        Log.Information("Updating stage data from web...");
        var stageManager = App.ServicesProvider.GetRequiredService<Services.StageManager>();
        await stageManager.UpdateStageWeb();
    }

    [RelayCommand]
    private void Save()
    {
        Log.Information("Saving settings...");

        _config.GameSettings.AdbPath = AdbPath;
        _config.GameSettings.Address = Address;
        _config.GameSettings.ClientType = ClientType;
        _config.GUI.Language = Language;

        ConfigFactory.SaveImmediately();
    }

    partial void OnLanguageChanged(string value)
    {
        // For Phase 1, we might need a restart for language change or implement dynamic loading
        Log.Information("Language changed to: {Language}", value);
    }
}
