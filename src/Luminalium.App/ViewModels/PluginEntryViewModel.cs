using Luminalium.Plugins;

namespace Luminalium.App.ViewModels;

public sealed class PluginEntryViewModel(PluginMetadata metadata)
{
    public string Id { get; } = metadata.Id;

    public string DisplayName { get; } = string.IsNullOrWhiteSpace(metadata.DisplayName)
        ? metadata.Id
        : metadata.DisplayName;

    public string RawDisplayName { get; } = metadata.DisplayName;

    public PluginType PluginType { get; } = metadata.PluginType;

    public string PluginTypeText { get; } = metadata.PluginType.ToString();

    public string IconKey { get; } = string.IsNullOrWhiteSpace(metadata.IconKey)
        ? "No icon key"
        : metadata.IconKey;

    public string Description { get; } = string.IsNullOrWhiteSpace(metadata.Description)
        ? "Plugin surface is reserved for Tasks 17-19."
        : metadata.Description;

    public string Version { get; } = metadata.Version;
}
