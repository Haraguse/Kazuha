using Avalonia.Controls;

namespace Luminalium.Plugins;

public static class BuiltInPluginCatalog
{
    private const string PlaceholderDetail = "Implemented in Tasks 17-19.";

    /// <summary>
    /// Creates the deterministic built-in plugin registry. This is the only Task 11 registration authority.
    /// </summary>

    public static BuiltInPluginRegistry CreateDefaultRegistry()
    {
        var registry = new BuiltInPluginRegistry();

        Register(registry, new PluginMetadata(
            "settings",
            "",
            PluginType.Toolbar,
            "settings.svg",
            "Luminalium 的核心设置插件。"));
        Register(registry, new PluginMetadata(
            "onboarding",
            "Onboarding",
            PluginType.Window,
            ""));
        Register(registry, new PluginMetadata(
            "board",
            "板中板 - Luminalium",
            PluginType.Window,
            "board-in-board.svg",
            "自由书写的黑板"));
        Register(registry, new PluginMetadata(
            "timer",
            "Timer",
            PluginType.Toolbar,
            "timer.svg",
            "Luminalium 的定时器插件。"));
        Register(registry, new PluginMetadata(
            "spotlight",
            "Spotlight",
            PluginType.Toolbar,
            "spotlight.svg",
            "Luminalium 的聚光灯插件，支持区域高亮、关灯模式和放大镜。"));
        Register(registry, new PluginMetadata(
            "app_launcher",
            "App Launcher",
            PluginType.ToolbarMulti,
            "apps.svg",
            "Launch applications from the toolbar."));
        Register(registry, new PluginMetadata(
            "logs",
            "日志 - Luminalium",
            PluginType.Window,
            "debug.svg",
            "查看应用日志和系统信息"));
        Register(registry, new PluginMetadata(
            "status_bar",
            "Status Bar",
            PluginType.StatusBar,
            "",
            "Luminalium 的状态栏插件。"));

        return registry;
    }

    private static void Register(BuiltInPluginRegistry registry, PluginMetadata metadata)
    {
        var plugin = new BuiltInPlugin(
            metadata,
            metadata.Id is "onboarding" or "logs"
                ? new CompletedPluginCommand()
                : new PlaceholderPluginCommand(metadata.Id),
            metadata.Id is "onboarding" or "logs"
                ? new NativeSurfaceViewFactory(metadata.Id)
                : new PlaceholderPluginViewFactory(metadata.DisplayName.Length == 0 ? metadata.Id : metadata.DisplayName));
        var result = registry.Register(plugin);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error!.Message);
        }
    }

    private sealed class PlaceholderPluginCommand(string pluginId) : IPluginCommand
    {
        // Real built-in plugin behavior is intentionally deferred to Tasks 17-19.
        public Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            return Task.FromResult(PluginExecutionResult.Failure(new PluginExecutionError(
                PluginExecutionErrorCode.NotSupported,
                $"Built-in plugin '{pluginId}' has placeholder command behavior. {PlaceholderDetail}",
                pluginId,
                PlaceholderDetail)));
        }

        public Task TerminateAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class CompletedPluginCommand : IPluginCommand
    {
        public Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken) =>
            Task.FromResult(PluginExecutionResult.Success());

        public Task TerminateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PlaceholderPluginViewFactory(string label) : IPluginViewFactory
    {
        // Real built-in plugin views are intentionally deferred to Tasks 17-19.
        public Control CreateView() =>
            new TextBlock { Text = $"{label} placeholder view. {PlaceholderDetail}" };
    }

    private sealed class NativeSurfaceViewFactory(string pluginId) : IPluginViewFactory
    {
        public Control CreateView() => new ContentControl
        {
            Tag = pluginId,
        };
    }
}
