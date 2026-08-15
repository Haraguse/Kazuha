using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class SplashStyleOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly string _displayNameKey;

    public SplashStyleOptionViewModel(string style, string displayNameKey, ILocalizationService localization)
    {
        Style = style;
        _displayNameKey = displayNameKey;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public string Style { get; }

    public string DisplayName => _localization[_displayNameKey];

    public override string ToString() => DisplayName;
}
