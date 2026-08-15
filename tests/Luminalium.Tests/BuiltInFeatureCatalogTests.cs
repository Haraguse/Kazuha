using Luminalium.App.Features;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureCatalogTests
{
    [Fact]
    public void DefaultCatalogExposesEightFeaturesInStableOrder()
    {
        var catalog = BuiltInFeatureCatalog.Default;

        Assert.Equal(8, catalog.Count);
        Assert.Equal(
        [
            "settings",
            "onboarding",
            "board",
            "timer",
            "spotlight",
            "app_launcher",
            "logs",
            "status_bar",
        ], catalog.Descriptors.Select(descriptor => descriptor.Id.Value));
        Assert.Equal(8, catalog.Descriptors.Select(descriptor => descriptor.Id).Distinct().Count());
        Assert.All(catalog.Descriptors, descriptor =>
        {
            Assert.Equal(descriptor.Id.Value, descriptor.RouteKey);
            Assert.Equal($"built-in-feature.{descriptor.Id.Value}", descriptor.DiagnosticsKey);
            Assert.True(descriptor.IsAvailable());
        });
    }

    [Fact]
    public void DefaultCatalogDefinesExpectedSurfacesAndActivationModes()
    {
        var descriptors = BuiltInFeatureCatalog.Default.Descriptors.ToDictionary(item => item.Id);

        AssertDescriptor(descriptors[BuiltInFeatureId.Settings], BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage);
        AssertDescriptor(descriptors[BuiltInFeatureId.Onboarding], BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage);
        AssertDescriptor(descriptors[BuiltInFeatureId.Logs], BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage);
        AssertDescriptor(descriptors[BuiltInFeatureId.Board], BuiltInFeatureSurface.Window, BuiltInFeatureActivationMode.FreshWindow);
        AssertDescriptor(descriptors[BuiltInFeatureId.Timer], BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow);
        AssertDescriptor(descriptors[BuiltInFeatureId.Spotlight], BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow);
        AssertDescriptor(descriptors[BuiltInFeatureId.AppLauncher], BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow);
        AssertDescriptor(descriptors[BuiltInFeatureId.StatusBar], BuiltInFeatureSurface.StatusBar, BuiltInFeatureActivationMode.FreshWindow);
    }

    [Fact]
    public void CatalogRejectsDuplicateIdsWithoutExposingPartialState()
    {
        var timer = BuiltInFeatureCatalog.Default.Descriptors.Single(item => item.Id == BuiltInFeatureId.Timer);

        var exception = Assert.Throws<ArgumentException>(() => new BuiltInFeatureCatalog([timer, timer]));

        Assert.Equal("descriptors", exception.ParamName);
        Assert.Contains("Duplicate built-in feature id 'timer'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DescriptorRejectsMalformedMetadataDeterministically()
    {
        var routeException = Assert.Throws<ArgumentException>(() => new BuiltInFeatureDescriptor(
            BuiltInFeatureId.Timer,
            "Plugin.timer.Name",
            "timer.svg",
            BuiltInFeatureSurface.Toolbar,
            BuiltInFeatureActivationMode.FreshWindow,
            "plugin:timer",
            static () => true,
            "built-in-feature.timer",
            "Timer",
            "Timer"));
        var modeException = Assert.Throws<ArgumentException>(() => new BuiltInFeatureDescriptor(
            BuiltInFeatureId.Settings,
            "Plugin.settings.Name",
            "settings.svg",
            BuiltInFeatureSurface.ShellPage,
            BuiltInFeatureActivationMode.FreshWindow,
            "settings",
            static () => true,
            "built-in-feature.settings",
            "Settings",
            "Settings"));

        Assert.Equal("routeKey", routeException.ParamName);
        Assert.Equal("activationMode", modeException.ParamName);
    }

    [Theory]
    [InlineData("timer", true)]
    [InlineData("Timer", false)]
    [InlineData(" timer", false)]
    [InlineData("timer ", false)]
    [InlineData("plugin:timer", false)]
    [InlineData("", false)]
    public void CanonicalIdentityParsingIsOrdinalAndDoesNotTrim(string input, bool expected)
    {
        Assert.Equal(expected, BuiltInFeatureId.TryParseCanonical(input, out var id));
        if (expected)
        {
            Assert.Equal(BuiltInFeatureId.Timer, id);
        }
    }

    private static void AssertDescriptor(
        BuiltInFeatureDescriptor descriptor,
        BuiltInFeatureSurface surface,
        BuiltInFeatureActivationMode activationMode)
    {
        Assert.Equal(surface, descriptor.Surface);
        Assert.Equal(activationMode, descriptor.ActivationMode);
    }
}
