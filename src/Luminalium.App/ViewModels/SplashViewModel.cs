using Luminalium.Core.Identity;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class SplashViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly SplashPolicyResult _policy;

    public SplashViewModel(string versionDisplay)
        : this(versionDisplay, new LocalizationService(), new SplashPolicyResult(true, SplashPolicyService.DefaultStyle, false))
    {
    }

    public SplashViewModel(string versionDisplay, ILocalizationService localization)
        : this(versionDisplay, localization, new SplashPolicyResult(true, SplashPolicyService.DefaultStyle, false))
    {
    }

    public SplashViewModel(
        string versionDisplay,
        ILocalizationService localization,
        SplashPolicyService policyService,
        GeneralSettings settings,
        bool isAutoStart,
        TimeOnly currentTime)
        : this(versionDisplay, localization, policyService.Evaluate(settings, isAutoStart, currentTime))
    {
    }

    public SplashViewModel(string versionDisplay, ILocalizationService localization, SplashPolicyResult policy)
    {
        VersionDisplay = versionDisplay;
        _localization = localization;
        _policy = policy;
        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(StartupText));
            OnPropertyChanged(nameof(DetailText));
            OnPropertyChanged(nameof(StyleText));
        };
    }

    public string ProductName { get; } = ProductIdentity.DisplayName;

    public string VersionDisplay { get; }

    public string StartupText => _localization["Splash.StartupText"];

    public string DetailText => _localization[_policy.ShowDetailedSplash ? "Splash.DetailText" : "Splash.BriefText"];

    public string StyleText => _localization[_policy.Style == "nina_iseri_1_2"
        ? "Splash.Style.NinaIseri"
        : "Splash.Style.Default"];

    public bool ShowDetailedSplash => _policy.ShowDetailedSplash;

    public string Style => _policy.Style;
}
