using System;
using System.IO;
using MaaGui.Configuration.Factory;
using Xunit;

namespace MaaGui.Tests;

public class ConfigFactoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalDir;

    public ConfigFactoryTests()
    {
        _originalDir = ConfigFactory.ConfigDir;
        _tempDir = Path.Combine(Path.GetTempPath(), "MaaGuiTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        ConfigFactory.SetConfigDir(_tempDir);
    }

    public void Dispose()
    {
        ConfigFactory.SetConfigDir(_originalDir);
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void TestDefaultConfig()
    {
        var config = ConfigFactory.Load();
        Assert.NotNull(config);
        Assert.Equal("Default", config.Current);
        Assert.NotEmpty(config.Configurations);
        Assert.Equal("Default", config.Configurations[0].Name);
    }

    [Fact]
    public void TestConfigSaveLoad()
    {
        var config = ConfigFactory.Load();
        config.GameSettings.Address = "test_address";
        
        ConfigFactory.SaveImmediately();
        
        var reloaded = ConfigFactory.Load();
        Assert.Equal("test_address", reloaded.GameSettings.Address);
    }
}
