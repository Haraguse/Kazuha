namespace Luminalium.App.ViewModels;

public sealed class PluginPageViewModel(PluginEntryViewModel plugin) : ShellPageViewModel($"plugin:{plugin.Id}", plugin.DisplayName)
{
    public PluginEntryViewModel Plugin { get; } = plugin;

    public string PlaceholderNotice { get; } = "The native plugin view factory surface is reserved for Tasks 17-19.";
}
