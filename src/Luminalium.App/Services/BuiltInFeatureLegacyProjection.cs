using Luminalium.App.Features;
using Luminalium.Plugins;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public sealed class BuiltInFeatureLegacyProjection
{
    private readonly BuiltInFeatureCatalog _catalog;

    public BuiltInFeatureLegacyProjection(BuiltInFeatureCatalog? catalog = null, BuiltInFeatureMigrationDiagnostics? diagnostics = null)
    {
        _catalog = catalog ?? BuiltInFeatureCatalog.Default;
        Diagnostics = diagnostics ?? new BuiltInFeatureMigrationDiagnostics();
    }

    public BuiltInFeatureMigrationDiagnostics Diagnostics { get; }

    public IReadOnlyList<PluginMetadata> Metadata
    {
        get
        {
            Diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed);
            return _catalog.Descriptors.Select(ToMetadata).ToArray();
        }
    }

    public IReadOnlyList<BuiltInFeatureEntryViewModel> Entries(Luminalium.Core.Localization.ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        Diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed);
        return _catalog.Descriptors
            .Select(descriptor => new BuiltInFeatureEntryViewModel(descriptor, localization))
            .ToArray();
    }

    private static PluginMetadata ToMetadata(BuiltInFeatureDescriptor descriptor) =>
        new(
            descriptor.Id.Value,
            descriptor.FallbackDisplayName,
            ToPluginType(descriptor),
            descriptor.IconKey,
            descriptor.FallbackDescription);

    private static PluginType ToPluginType(BuiltInFeatureDescriptor descriptor) =>
        descriptor.Id == BuiltInFeatureId.AppLauncher
            ? PluginType.ToolbarMulti
            : descriptor.Surface switch
            {
                BuiltInFeatureSurface.ShellPage when descriptor.Id is var id &&
                    (id == BuiltInFeatureId.Onboarding || id == BuiltInFeatureId.Logs) => PluginType.Window,
                BuiltInFeatureSurface.StatusBar => PluginType.StatusBar,
                BuiltInFeatureSurface.Window => PluginType.Window,
                _ => PluginType.Toolbar,
            };
}
