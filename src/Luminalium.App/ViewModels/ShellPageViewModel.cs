using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public abstract class ShellPageViewModel : ObservableObject
{
    private readonly string? _titleKey;
    private readonly string _fallbackTitle;

    protected ShellPageViewModel(string navigationKey, string titleKey, ILocalizationService localization)
    {
        NavigationKey = navigationKey;
        _titleKey = titleKey;
        _fallbackTitle = titleKey;
        Localization = localization;
        Localization.LanguageChanged += OnLanguageChanged;
    }

    protected ShellPageViewModel(string navigationKey, string fallbackTitle)
    {
        NavigationKey = navigationKey;
        _fallbackTitle = fallbackTitle;
        Localization = new LocalizationService();
    }

    public string NavigationKey { get; }

    public virtual string Title => _titleKey is null ? _fallbackTitle : Localization[_titleKey];

    protected ILocalizationService Localization { get; }

    protected virtual void RefreshLocalizedText() => OnPropertyChanged(nameof(Title));

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs args) => RefreshLocalizedText();
}
