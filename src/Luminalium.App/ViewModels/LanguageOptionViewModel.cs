using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class LanguageOptionViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly string _displayNameKey;

    public LanguageOptionViewModel(AppLanguage language, string displayNameKey, ILocalizationService localization)
    {
        Language = language;
        _displayNameKey = displayNameKey;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public AppLanguage Language { get; }

    public string Code => Language.ToCode();

    public string DisplayName => _localization[_displayNameKey];

    public override string ToString() => DisplayName;
}
