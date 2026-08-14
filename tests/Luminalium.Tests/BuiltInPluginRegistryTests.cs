using Avalonia.Controls;
using Luminalium.Plugins;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInPluginRegistryTests
{
    private static readonly string[] ExpectedDefaultOrder =
    [
        "settings",
        "onboarding",
        "board",
        "timer",
        "spotlight",
        "app_launcher",
        "logs",
        "status_bar",
    ];

    [Fact]
    public void DefaultCatalogRegistersLegacyBuiltInsInManifestOrder()
    {
        var registry = BuiltInPluginCatalog.CreateDefaultRegistry();
        var plugins = registry.Enumerate();

        Assert.Equal(8, registry.Count);
        Assert.Equal(ExpectedDefaultOrder, plugins.Select(plugin => plugin.Metadata.Id));
        Assert.Equal(8, plugins.Select(plugin => plugin.Metadata.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(plugins, plugin => Assert.False(string.IsNullOrWhiteSpace(plugin.Metadata.Id)));

        AssertMetadata(plugins[0], "settings", "", PluginType.Toolbar, "settings.svg");
        AssertMetadata(plugins[1], "onboarding", "Onboarding", PluginType.Window, "");
        AssertMetadata(plugins[2], "board", "板中板 - Luminalium", PluginType.Window, "board-in-board.svg");
        AssertMetadata(plugins[3], "timer", "Timer", PluginType.Toolbar, "timer.svg");
        AssertMetadata(plugins[4], "spotlight", "Spotlight", PluginType.Toolbar, "spotlight.svg");
        AssertMetadata(plugins[5], "app_launcher", "App Launcher", PluginType.ToolbarMulti, "apps.svg");
        AssertMetadata(plugins[6], "logs", "日志 - Luminalium", PluginType.Window, "debug.svg");
        AssertMetadata(plugins[7], "status_bar", "Status Bar", PluginType.StatusBar, "");
    }

    [Fact]
    public async Task DefaultCatalogUsesPlaceholderCommandsAndViewsUntilLaterTasks()
    {
        var registry = BuiltInPluginCatalog.CreateDefaultRegistry();
        var plugin = registry.Get("timer")!;
        var result = await plugin.ExecuteAsync(new PluginContext(new RecordingHost(), CancellationToken.None));
        var view = plugin.ViewFactory.CreateView();

        Assert.False(result.IsSuccess);
        Assert.Equal(PluginExecutionErrorCode.NotSupported, result.Error!.Code);
        Assert.Equal("timer", result.Error.PluginId);
        Assert.Contains("Tasks 17-19", result.Error.Detail, StringComparison.Ordinal);
        var textBlock = Assert.IsType<TextBlock>(view);
        Assert.Contains("Tasks 17-19", textBlock.Text, StringComparison.Ordinal);
        Assert.False(plugin.IsActive);
    }

    [Fact]
    public void DuplicateRegistrationReturnsTypedErrorAndKeepsOriginal()
    {
        var registry = new BuiltInPluginRegistry();
        var original = Plugin("timer", new RecordingCommand());
        var duplicate = Plugin("timer", new RecordingCommand());

        Assert.True(registry.Register(original).IsSuccess);
        var duplicateResult = registry.Register(duplicate);

        Assert.False(duplicateResult.IsSuccess);
        var error = Assert.IsType<DuplicateRegistrationError>(duplicateResult.Error);
        Assert.Equal("timer", error.DuplicateId);
        Assert.Same(original, registry.Get("timer"));
        Assert.Equal(1, registry.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidRegistrationIdsReturnTypedValidationError(string? id)
    {
        var registry = new BuiltInPluginRegistry();
        var result = registry.Register(Plugin(id, new RecordingCommand()));

        Assert.False(result.IsSuccess);
        Assert.IsType<PluginRegistrationValidationError>(result.Error);
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public async Task TerminateAllInvokesEveryPluginAndAggregatesFailures()
    {
        var registry = new BuiltInPluginRegistry();
        var first = new RecordingCommand();
        var throwing = new RecordingCommand(throwOnTerminate: true);
        var last = new RecordingCommand();
        Assert.True(registry.Register(Plugin("first", first)).IsSuccess);
        Assert.True(registry.Register(Plugin("throwing", throwing)).IsSuccess);
        Assert.True(registry.Register(Plugin("last", last)).IsSuccess);

        var result = await registry.TerminateAllAsync();

        Assert.False(result.IsSuccess);
        var aggregate = Assert.IsType<PluginTerminationAggregateError>(result.Error);
        var failure = Assert.Single(aggregate.Failures);
        Assert.Equal("throwing", failure.PluginId);
        Assert.Contains("terminate failed", failure.Detail, StringComparison.Ordinal);
        Assert.Equal(1, first.TerminateCount);
        Assert.Equal(1, throwing.TerminateCount);
        Assert.Equal(1, last.TerminateCount);
    }

    [Fact]
    public async Task TerminateAllReturnsSuccessWhenEveryPluginTerminates()
    {
        var registry = new BuiltInPluginRegistry();
        var first = new RecordingCommand();
        var second = new RecordingCommand();
        Assert.True(registry.Register(Plugin("first", first)).IsSuccess);
        Assert.True(registry.Register(Plugin("second", second)).IsSuccess);

        var result = await registry.TerminateAllAsync();

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(1, first.TerminateCount);
        Assert.Equal(1, second.TerminateCount);
    }

    [Fact]
    public async Task ExecuteGuardsConcurrentActivationAndTerminateIsIdempotent()
    {
        var command = new RecordingCommand(waitForCompletion: true);
        var plugin = Plugin("timer", command);
        var context = new PluginContext(new RecordingHost(), CancellationToken.None);
        var running = plugin.ExecuteAsync(context);

        Assert.True(await command.ExecuteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(plugin.IsActive);

        var second = await plugin.ExecuteAsync(context);
        Assert.False(second.IsSuccess);
        Assert.Equal(PluginExecutionErrorCode.ConcurrentActivation, second.Error!.Code);
        Assert.Equal(1, command.ExecuteCount);

        await plugin.TerminateAsync();
        await plugin.TerminateAsync();
        Assert.False(plugin.IsActive);
        Assert.Equal(2, command.TerminateCount);

        command.CompleteExecute();
        var first = await running;
        Assert.True(first.IsSuccess, first.Error?.Message);
    }

    [Fact]
    public void CatalogOrderIsStableAcrossFreshRegistries()
    {
        var first = BuiltInPluginCatalog.CreateDefaultRegistry().Enumerate();
        var second = BuiltInPluginCatalog.CreateDefaultRegistry().Enumerate();

        Assert.Equal(
            first.Select(plugin => (plugin.Metadata.Id, plugin.Metadata.DisplayName, plugin.Metadata.PluginType, plugin.Metadata.IconKey)),
            second.Select(plugin => (plugin.Metadata.Id, plugin.Metadata.DisplayName, plugin.Metadata.PluginType, plugin.Metadata.IconKey)));
    }

    [Theory]
    [InlineData(null, PluginType.Toolbar)]
    [InlineData("", PluginType.Toolbar)]
    [InlineData("toolbar", PluginType.Toolbar)]
    [InlineData("toolbar_multi", PluginType.ToolbarMulti)]
    [InlineData("window", PluginType.Window)]
    [InlineData("status_bar", PluginType.StatusBar)]
    public void PluginTypeMapsLegacyManifestStrings(string? legacyType, PluginType expected)
    {
        Assert.Equal(expected, PluginTypeMappings.FromLegacyString(legacyType));
        Assert.Equal(legacyType is null or "" ? "toolbar" : legacyType, expected.ToLegacyString());
    }

    private static void AssertMetadata(
        BuiltInPlugin plugin,
        string id,
        string displayName,
        PluginType pluginType,
        string iconKey)
    {
        Assert.Equal(id, plugin.Metadata.Id);
        Assert.Equal(displayName, plugin.Metadata.DisplayName);
        Assert.Equal(pluginType, plugin.Metadata.PluginType);
        Assert.Equal(iconKey, plugin.Metadata.IconKey);
        Assert.Equal("1.0.0", plugin.Metadata.Version);
    }

    private static BuiltInPlugin Plugin(string? id, IPluginCommand command) =>
        new(new PluginMetadata(id, id, PluginType.Toolbar, "test.svg"), command, new StaticViewFactory());

    private sealed class StaticViewFactory : IPluginViewFactory
    {
        public Control CreateView() =>
            new Control();
    }

    private sealed class RecordingHost : IPluginHost
    {
        public Task TerminatePluginAsync(string pluginId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingCommand(bool throwOnTerminate = false, bool waitForCompletion = false) : IPluginCommand
    {
        private readonly TaskCompletionSource<bool> _executeRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ExecuteCount { get; private set; }

        public int TerminateCount { get; private set; }

        public TaskCompletionSource<bool> ExecuteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ExecuteCount++;
            ExecuteStarted.TrySetResult(true);

            if (waitForCompletion)
            {
                await _executeRelease.Task.WaitAsync(cancellationToken);
            }

            return PluginExecutionResult.Success();
        }

        public Task TerminateAsync(CancellationToken cancellationToken)
        {
            TerminateCount++;
            if (throwOnTerminate)
            {
                throw new InvalidOperationException("terminate failed");
            }

            return Task.CompletedTask;
        }

        public void CompleteExecute() =>
            _executeRelease.TrySetResult(true);
    }
}
