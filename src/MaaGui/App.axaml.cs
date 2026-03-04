using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using MaaGui.Services.MaaInterop;
using MaaGui.Platform;
using MaaGui.ViewModels;
using MaaGui.Views;
using MaaGui.Configuration.Factory;

namespace MaaGui;

public partial class App : Application
{
    public static new App Current => (App)Avalonia.Application.Current!;
    public IServiceProvider Services { get; private set; } = null!;
    public static IServiceProvider ServicesProvider => Current.Services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Ensure debug dir exists
        if (!Directory.Exists(Helper.PathsHelper.DebugDir))
        {
            Directory.CreateDirectory(Helper.PathsHelper.DebugDir);
        }

        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(Helper.PathsHelper.DebugDir, "gui.log"), rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .CreateLogger();

        // Register services
        ConfigureServices(services);

        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<Views.MainWindow>();
        services.AddTransient<TaskQueueViewModel>();
        services.AddTransient<ViewModels.Copilot.CopilotViewModel>();
        services.AddTransient<ViewModels.Utilities.UtilitiesViewModel>();
        services.AddTransient<ViewModels.Settings.SettingsViewModel>();

        // Build service provider
        Services = services.BuildServiceProvider();

        // Initialize AsstProxy
        var asstProxy = Services.GetRequiredService<AsstProxy>();
        asstProxy.Initialize(Helper.PathsHelper.BaseDir, Helper.PathsHelper.UserDataDir);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<Views.MainWindow>();
            desktop.Exit += OnApplicationExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Log.Information("Application is shutting down");

        // Dispose ConfigFactory timer
        try
        {
            ConfigFactory.Dispose();
            Log.Information("ConfigFactory disposed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error disposing ConfigFactory");
        }

        // Dispose AsstProxy
        try
        {
            var asstProxy = Services.GetService<AsstProxy>();
            asstProxy?.Dispose();
            Log.Information("AsstProxy disposed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error disposing AsstProxy");
        }

        // Dispose the service provider
        if (Services is IDisposable disposableServices)
        {
            try
            {
                disposableServices.Dispose();
                Log.Information("Service provider disposed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error disposing service provider");
            }
        }

        Log.CloseAndFlush();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration - singleton from factory
        services.AddSingleton<Configuration.Root>(s => Configuration.Factory.ConfigFactory.CurrentConfig);

        // MaaCore Interop
        services.AddSingleton<IMaaService, MaaService>();
        services.AddSingleton<AsstProxy>();

        // Platform Services (registered based on platform)
        RegisterPlatformServices(services);

        // Notification Services
        services.AddSingleton<Services.Notification.INotificationPoster, Services.Notification.FallbackNotificationPoster>();

        // HTTP Services
        services.AddSingleton<Services.Web.IHttpService, Services.Web.HttpService>();
        services.AddSingleton<Services.Web.IMaaApiService, Services.Web.MaaApiService>();

        // Stage Manager
        services.AddSingleton<Services.StageManager>();

        // Localization
        services.AddSingleton<Localization.LocalizationHelper>();
    }

    private void RegisterPlatformServices(IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<Platform.IPlatformServices, Platform.Windows.WindowsPlatformServices>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<Platform.IPlatformServices, Platform.macOS.MacPlatformServices>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<Platform.IPlatformServices, Platform.Linux.LinuxPlatformServices>();
        }
    }
}
