using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Features;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class BuiltInFeatureEntryViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

    public BuiltInFeatureEntryViewModel(BuiltInFeatureDescriptor descriptor, ILocalizationService localization)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _localization.LanguageChanged += (_, _) => RefreshLocalizedText();
    }

    public BuiltInFeatureDescriptor Descriptor { get; }
    public string Id => Descriptor.Id.Value;
    public BuiltInFeatureId FeatureId => Descriptor.Id;
    public string RouteKey => Descriptor.RouteKey;
    public string DisplayName => LocalizedValue("Name", string.IsNullOrWhiteSpace(Descriptor.FallbackDisplayName) ? FeatureId.Value : Descriptor.FallbackDisplayName);
    public string RawDisplayName => Descriptor.FallbackDisplayName;
    public string FeatureTypeText => _localization[$"Plugin.Type.{LegacyTypeKey()}"];
    public string IconKey => string.IsNullOrWhiteSpace(Descriptor.IconKey) ? _localization["Plugin.NoIconKey"] : Descriptor.IconKey;
    public string Description => string.IsNullOrWhiteSpace(Descriptor.FallbackDescription)
        ? _localization["Plugin.FallbackDescription"]
        : LocalizedValue("Description", Descriptor.FallbackDescription);

    private string LocalizedValue(string field, string fallback)
    {
        var key = $"Plugin.{Id}.{field}";
        var localized = _localization[key];
        return StringComparer.Ordinal.Equals(localized, key) ? fallback : localized;
    }

    private string LegacyTypeKey() => FeatureId == BuiltInFeatureId.AppLauncher
        ? "ToolbarMulti"
        : FeatureId is var feature && (feature == BuiltInFeatureId.Onboarding || feature == BuiltInFeatureId.Board || feature == BuiltInFeatureId.Logs)
            ? "Window"
            : Descriptor.Surface switch
            {
                BuiltInFeatureSurface.StatusBar => "StatusBar",
                _ => "Toolbar",
            };

    private void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(FeatureTypeText));
        OnPropertyChanged(nameof(IconKey));
        OnPropertyChanged(nameof(Description));
    }
}
