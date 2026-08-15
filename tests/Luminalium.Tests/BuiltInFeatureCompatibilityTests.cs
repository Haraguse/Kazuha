using Luminalium.App.Features;
using Luminalium.App.Services;
using Luminalium.Core.Localization;
using Luminalium.Plugins;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureCompatibilityTests
{
    [Fact]
    public void ProjectionPreservesLegacyOrderAndMetadata()
    {
        var projection = new BuiltInFeatureLegacyProjection();
        var metadata = projection.Metadata;

        Assert.Equal(["settings", "onboarding", "board", "timer", "spotlight", "app_launcher", "logs", "status_bar"], metadata.Select(item => item.Id));
        Assert.Equal("board-in-board.svg", metadata.Single(item => item.Id == "board").IconKey);
        Assert.Equal(PluginType.ToolbarMulti, metadata.Single(item => item.Id == "app_launcher").PluginType);
        Assert.Equal(PluginType.StatusBar, metadata.Single(item => item.Id == "status_bar").PluginType);
    }

    [Fact]
    public void ProjectionIsFreshReadOnlyDataAndCannotMutateNativeCatalog()
    {
        var projection = new BuiltInFeatureLegacyProjection();
        var first = projection.Metadata;
        var second = projection.Metadata;

        Assert.NotSame(first, second);
        Assert.Equal(BuiltInFeatureCatalog.Default.Descriptors.Select(item => item.Id.Value), second.Select(item => item.Id));
        Assert.Equal(2, projection.Diagnostics.Count(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed));
    }

    [Fact]
    public void NativeCatalogDoesNotCreateLegacyPluginObjectsDuringConstruction()
    {
        var catalog = BuiltInFeatureCatalog.Default;

        Assert.All(catalog.Descriptors, descriptor => Assert.IsType<BuiltInFeatureDescriptor>(descriptor));
        Assert.DoesNotContain(catalog.Descriptors, descriptor => descriptor.GetType() == typeof(PluginMetadata));
    }

    [Fact]
    public void EntryProjectionUsesNativeDescriptorsAsItsOnlySource()
    {
        var projection = new BuiltInFeatureLegacyProjection();
        var entries = projection.Entries(new LocalizationService());

        Assert.Equal(BuiltInFeatureCatalog.Default.Descriptors.Select(item => item.Id.Value), entries.Select(item => item.Id));
        Assert.Equal(1, projection.Diagnostics.Count(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed));
    }
}
