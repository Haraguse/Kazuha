using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class AccentOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly string _displayNameKey;

    public AccentOptionViewModel(string key, string displayNameKey, ILocalizationService localization)
    {
        Key = key;
        _displayNameKey = displayNameKey;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public string Key { get; }

    public string DisplayName => _localization[_displayNameKey];

    public override string ToString() => DisplayName;
}
