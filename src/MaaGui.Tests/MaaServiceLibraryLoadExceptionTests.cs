using System;
using System.Reflection;
using MaaGui.Services.MaaInterop;
using Xunit;

namespace MaaGui.Tests;

public class MaaServiceLibraryLoadExceptionTests
{
    private static DllNotFoundException CreateException(string configuredLibraryPath, string platformLibraryName, bool configuredPathExists, Exception innerException)
    {
        var method = typeof(MaaService).GetMethod(
            "CreateMaaCoreLoadException",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method!.Invoke(
            null,
            new object[] { configuredLibraryPath, platformLibraryName, configuredPathExists, innerException });

        return Assert.IsType<DllNotFoundException>(result);
    }

    [Fact]
    public void CreateMaaCoreLoadException_WhenConfiguredPathMissing_IncludesActionableGuidance()
    {
        var configuredPath = "/tmp/missing/libMaaCore.dylib";
        var inner = new DllNotFoundException("could not load");

        var exception = CreateException(configuredPath, "libMaaCore.dylib", false, inner);

        Assert.Contains("Failed to load MaaCore native library 'libMaaCore.dylib'", exception.Message);
        Assert.Contains(configuredPath, exception.Message);
        Assert.Contains("cmake --install build", exception.Message);
        Assert.Contains("resource", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void CreateMaaCoreLoadException_WhenConfiguredPathExists_ExplainsDependencyFailure()
    {
        var configuredPath = "/tmp/found/libMaaCore.dylib";
        var inner = new DllNotFoundException("missing dependency");

        var exception = CreateException(configuredPath, "libMaaCore.dylib", true, inner);

        Assert.Contains("Found '/tmp/found/libMaaCore.dylib', but loading it failed", exception.Message);
        Assert.Contains("transitive native dependency", exception.Message);
        Assert.Contains("Original loader error: missing dependency", exception.Message);
    }
}
