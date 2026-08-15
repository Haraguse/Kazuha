using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class OverviewViewModel : ShellPageViewModel
{
    private readonly string _versionDisplay;
    private readonly bool _versionUnavailable;

    public OverviewViewModel(
        string productName,
        string versionDisplay,
        bool versionUnavailable,
        IReadOnlyList<BuiltInFeatureEntryViewModel> builtInFeatures,
        ILocalizationService localization) : base("overview", "Navigation.Overview", localization)
    {
        ProductName = productName;
        _versionDisplay = versionDisplay;
        _versionUnavailable = versionUnavailable;
        BuiltInFeatures = builtInFeatures;
    }

    public string ProductName { get; }
    public string VersionDisplay => _versionUnavailable ? Localization["Shell.VersionUnavailable"] : _versionDisplay;
    public string NativeShellTitle => Localization["Overview.NativeShell.Title"];
    public string NativeShellBody => Localization["Overview.NativeShell.Body"];
    public string BuiltInPluginsTitle => Localization["Overview.BuiltInPlugins.Title"];
    public string BuiltInPluginListName => Localization["Overview.BuiltInPlugins.ListName"];
    public IReadOnlyList<BuiltInFeatureEntryViewModel> BuiltInFeatures { get; }

    protected override void RefreshLocalizedText()
    {
        base.RefreshLocalizedText();
        OnPropertyChanged(nameof(VersionDisplay));
        OnPropertyChanged(nameof(NativeShellTitle));
        OnPropertyChanged(nameof(NativeShellBody));
        OnPropertyChanged(nameof(BuiltInPluginsTitle));
        OnPropertyChanged(nameof(BuiltInPluginListName));
    }
}
