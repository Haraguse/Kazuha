namespace Luminalium.Plugins;

public sealed record PluginMetadata
{
    public PluginMetadata(
        string? id,
        string? displayName,
        PluginType pluginType,
        string? iconKey,
        string? description = "",
        string? version = "1.0.0")
    {
        Id = id?.Trim() ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        PluginType = pluginType;
        IconKey = iconKey ?? string.Empty;
        Description = description ?? string.Empty;
        Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public PluginType PluginType { get; }

    public string IconKey { get; }

    public string Description { get; }

    public string Version { get; }
}
