using System.Reflection;
using Luminalium.Core;
using Luminalium.Platform.Windows;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// Scaffold smoke tests (Task 3): prove the solution graph restores, builds,
/// and that project boundaries resolve. Deeper coverage arrives with Tasks 6-25.
/// </summary>
public class SolutionScaffoldTests
{
    [Fact]
    public void SolutionNameAndWindowsFloorAreStable()
    {
        Assert.Equal("Luminalium", SolutionInfo.SolutionName);
        Assert.Equal("10.0.17763.0", SolutionInfo.MinimumWindowsVersion);
    }

    [Fact]
    public void CoreAndPlatformWindowsAssembliesResolve()
    {
        Assert.Equal("Luminalium.Core", typeof(SolutionInfo).Assembly.GetName().Name);
        Assert.Equal("Luminalium.Platform.Windows", typeof(WindowsPlatformInfo).Assembly.GetName().Name);
    }

    [Fact]
    public void ValidatedWindowsFloorIsSupportedOnThisHost()
    {
        // Scaffold tests must never fail on hosts below the validated floor
        // (non-Windows CI or Windows 10 < build 17763): they prove project
        // structure, not the host OS. The assertion runs only when the host
        // actually meets the floor, keeping this green everywhere without a
        // SkippableFact dependency.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        Assert.True(WindowsPlatformInfo.IsSupportedWindowsVersion);
    }

    [Fact]
    public void PluginAndUpdaterAssembliesAreProduced()
    {
        // Simple-name loads prove the ProjectReference graph copied every assembly
        // into the test output (boundary wiring, not runtime discovery).
        Assert.NotNull(Assembly.Load("Luminalium.Plugins"));
        Assert.NotNull(Assembly.Load("Luminalium.Updater"));
    }
}
