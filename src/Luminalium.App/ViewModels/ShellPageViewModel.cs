using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public abstract class ShellPageViewModel : ObservableObject
{
    private readonly string? _titleKey;
    private readonly string _fallbackTitle;

    protected ShellPageViewModel(string titleKey, ILocalizationService localization)
    {
        _titleKey = titleKey;
        _fallbackTitle = titleKey;
        Localization = localization;
        Localization.LanguageChanged += OnLanguageChanged;
    }

    protected ShellPageViewModel(string fallbackTitle)
    {
        _fallbackTitle = fallbackTitle;
        Localization = new LocalizationService();
    }

    public virtual string Title => _titleKey is null ? _fallbackTitle : Localization[_titleKey];

    protected ILocalizationService Localization { get; }

    protected virtual void RefreshLocalizedText() => OnPropertyChanged(nameof(Title));

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs args) => RefreshLocalizedText();
}
