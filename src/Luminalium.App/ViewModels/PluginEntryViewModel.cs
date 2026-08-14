using Luminalium.Plugins;
using Luminalium.Core.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.App.ViewModels;

public sealed class PluginEntryViewModel : ObservableObject
{
    private readonly PluginMetadata _metadata;
    private readonly ILocalizationService _localization;

    public PluginEntryViewModel(PluginMetadata metadata, ILocalizationService localization)
    {
        _metadata = metadata;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => RefreshLocalizedText();
    }

    public string Id => _metadata.Id;

    public string DisplayName => LocalizedPluginValue("Name", string.IsNullOrWhiteSpace(_metadata.DisplayName) ? _metadata.Id : _metadata.DisplayName);

    public string RawDisplayName => _metadata.DisplayName;

    public PluginType PluginType => _metadata.PluginType;

    public string PluginTypeText => _localization[$"Plugin.Type.{_metadata.PluginType}"];

    public string IconKey => string.IsNullOrWhiteSpace(_metadata.IconKey)
        ? _localization["Plugin.NoIconKey"]
        : _metadata.IconKey;

    public string Description => string.IsNullOrWhiteSpace(_metadata.Description)
        ? _localization["Plugin.FallbackDescription"]
        : LocalizedPluginValue("Description", _metadata.Description);

    public string Version => _metadata.Version;

    private string LocalizedPluginValue(string field, string fallback)
    {
        var key = $"Plugin.{Id}.{field}";
        var localized = _localization[key];
        return StringComparer.Ordinal.Equals(localized, key) ? fallback : localized;
    }

    private void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(PluginTypeText));
        OnPropertyChanged(nameof(IconKey));
        OnPropertyChanged(nameof(Description));
    }
}
