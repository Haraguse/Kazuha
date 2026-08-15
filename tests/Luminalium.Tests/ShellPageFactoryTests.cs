using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// Focused tests for the native shell page mapping. The test project compiles
/// App source directly and cannot instantiate Avalonia XAML pages headlessly,
/// so the typed ViewModel-to-page-kind mapping is the tested seam; the factory
/// consumes that mapping to construct the real page controls.
/// </summary>
public sealed class ShellPageFactoryTests
{
    [Theory]
    [InlineData(typeof(OverviewViewModel), ShellPageKind.Overview)]
    [InlineData(typeof(SettingsViewModel), ShellPageKind.Settings)]
    [InlineData(typeof(OnboardingViewModel), ShellPageKind.Onboarding)]
    [InlineData(typeof(LogsViewModel), ShellPageKind.Logs)]
    public void NativeShellPagesResolveThroughTypedMapping(Type viewModelType, ShellPageKind expected)
    {
        Assert.Equal(expected, ShellPageMap.ResolvePageKind(viewModelType));
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(ShellViewModel))]
    [InlineData(typeof(ExtensionPageViewModel))]
    [InlineData(typeof(ShellPageViewModel))]
    public void UnknownOrPluginCarrierTargetsFailSafely(Type viewModelType)
    {
        Assert.Null(ShellPageMap.ResolvePageKind(viewModelType));
    }

    [Fact]
    public void ExtensionPageViewModelHasNoBuiltInPageConsumer()
    {
        Assert.Null(ShellPageMap.ResolvePageKind(typeof(ExtensionPageViewModel)));
    }
}
