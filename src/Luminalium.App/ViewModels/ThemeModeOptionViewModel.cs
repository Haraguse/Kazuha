using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class ThemeModeOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly string _displayNameKey;

    public ThemeModeOptionViewModel(ShellThemeMode mode, string displayNameKey, ILocalizationService localization)
    {
        Mode = mode;
        _displayNameKey = displayNameKey;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public ShellThemeMode Mode { get; }

    public string DisplayName => _localization[_displayNameKey];

    public override string ToString() => DisplayName;
}
