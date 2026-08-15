using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class SplashModeOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly string _displayNameKey;

    public SplashModeOptionViewModel(SplashMode mode, string displayNameKey, ILocalizationService localization)
    {
        Mode = mode;
        _displayNameKey = displayNameKey;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public SplashMode Mode { get; }

    public string DisplayName => _localization[_displayNameKey];

    public override string ToString() => DisplayName;
}
