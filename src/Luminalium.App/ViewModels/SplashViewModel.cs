using Luminalium.Core.Identity;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class SplashViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly ILocalizationService _localization;

    public SplashViewModel(string versionDisplay)
        : this(versionDisplay, new LocalizationService())
    {
    }

    public SplashViewModel(string versionDisplay, ILocalizationService localization)
    {
        VersionDisplay = versionDisplay;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(StartupText));
    }

    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string VersionDisplay { get; }

    public string StartupText => _localization["Splash.StartupText"];
}
