namespace Luminalium.Plugins;

public static class PluginTypeMappings
{
    public static PluginType FromLegacyString(string? legacyType) =>
        legacyType switch
        {
            null or "" or "toolbar" => PluginType.Toolbar,
            "toolbar_multi" => PluginType.ToolbarMulti,
            "window" => PluginType.Window,
            "status_bar" => PluginType.StatusBar,
            _ => throw new ArgumentOutOfRangeException(nameof(legacyType), legacyType, "Unknown legacy plugin type."),
        };

    public static string ToLegacyString(this PluginType pluginType) =>
        pluginType switch
        {
            PluginType.Toolbar => "toolbar",
            PluginType.ToolbarMulti => "toolbar_multi",
            PluginType.Window => "window",
            PluginType.StatusBar => "status_bar",
            _ => throw new ArgumentOutOfRangeException(nameof(pluginType), pluginType, "Unknown plugin type."),
        };
}
