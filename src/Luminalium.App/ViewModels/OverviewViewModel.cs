namespace Luminalium.App.ViewModels;

public sealed class OverviewViewModel(
    string productName,
    string versionDisplay,
    IReadOnlyList<PluginEntryViewModel> plugins) : ShellPageViewModel("overview", "Overview")
{
    public string ProductName { get; } = productName;

    public string VersionDisplay { get; } = versionDisplay;

    public IReadOnlyList<PluginEntryViewModel> Plugins { get; } = plugins;
}
