using Avalonia.Controls;
using Luminalium.App.Features;
using Luminalium.App.Services;
using Luminalium.Plugins;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// T12 boundary tests: a genuine external plugin built only from the
/// Luminalium.Plugins contracts registers, executes, renders, and terminates
/// with no access to the native feature catalog; an external ID colliding with
/// a canonical built-in ID is rejected deterministically; and the Plugins
/// assembly carries no App reference.
/// </summary>
public sealed class ExternalPluginBoundaryTests
{
    [Fact]
    public async Task GenuineExternalPluginRegistersExecutesRendersAndTerminatesWithoutCatalog()
    {
        var command = new SuccessfulCommand();
        var extension = new BuiltInPlugin(
            new PluginMetadata(
                "external.test.plugin",
                "Test Extension",
                PluginType.Toolbar,
                "test.svg",
                "Genuine third-party extension.",
                "2.0.0"),
            command,
            new TextViewFactory("external view"));

        // The external ID is not a canonical built-in ID, so it crosses the boundary.
        Assert.False(BuiltInFeatureId.TryParseCanonical(extension.Metadata.Id, out _));

        var boundary = new ExternalExtensionRegistry();
        var context = new PluginContext(new RecordingHost(), CancellationToken.None);

        Assert.True(boundary.Register(extension).IsSuccess);
        Assert.Equal(1, boundary.Count);
        Assert.Same(extension, boundary.Get("external.test.plugin"));
        Assert.Same(extension, Assert.Single(boundary.Enumerate()));

        var execute = await extension.ExecuteAsync(context);
        Assert.True(execute.IsSuccess, execute.Error?.Message);

        // The external command/view are the extension's own, never placeholders.
        Assert.IsType<SuccessfulCommand>(extension.Command);
        var view = Assert.IsType<TextBlock>(extension.ViewFactory.CreateView());
        Assert.Equal("external view", view.Text);

        var terminate = await boundary.TerminateAllAsync();
        Assert.True(terminate.IsSuccess, terminate.Error?.Message);
        Assert.Equal(1, command.TerminateCount);

        // The native catalog is untouched by a genuine external extension.
        Assert.Equal(8, BuiltInFeatureCatalog.Default.Count);
    }

    [Fact]
    public void ExternalPluginCollidingWithBuiltInIdIsRejectedDeterministically()
    {
        var collision = new BuiltInPlugin(
            new PluginMetadata(
                "timer",
                "Shadow Extension",
                PluginType.Toolbar,
                "timer.svg",
                "Attempted shadow of a built-in.",
                "1.0.0"),
            new SuccessfulCommand(),
            new TextViewFactory("shadow"));

        var boundary = new ExternalExtensionRegistry();
        var result = boundary.Register(collision);

        Assert.False(result.IsSuccess);
        var error = Assert.IsType<PluginRegistrationCollisionError>(result.Error);
        Assert.Equal("timer", error.CollisionId);
        Assert.Equal(0, boundary.Count);
        Assert.Null(boundary.Get("timer"));

        // The native catalog remains authoritative and unchanged.
        Assert.Equal(8, BuiltInFeatureCatalog.Default.Count);
        Assert.True(BuiltInFeatureCatalog.Default.TryGet(BuiltInFeatureId.Timer, out var canonical));
        Assert.Equal("Timer", canonical.FallbackDisplayName);
    }

    [Fact]
    public void PluginsAssemblyDoesNotReferenceAppAssembly()
    {
        var assembly = typeof(BuiltInPlugin).Assembly;

        Assert.Equal("Luminalium.Plugins", assembly.GetName().Name);

        var referenced = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Luminalium.App", referenced);
    }

    [Fact]
    public void NativeBuiltInsOperateWithoutAnyPluginRegistration()
    {
        // T15 final integration: the plugin registry and the app-facing
        // external extension registry both start empty. No built-in feature
        // is registered as a plugin; every one of the eight native features is
        // resolved solely from the typed native catalog.
        var pluginRegistry = new BuiltInPluginRegistry();
        var externalRegistry = new ExternalExtensionRegistry();

        Assert.Equal(0, pluginRegistry.Count);
        Assert.Equal(0, externalRegistry.Count);
        Assert.Equal(8, BuiltInFeatureCatalog.Default.Count);

        foreach (var descriptor in BuiltInFeatureCatalog.Default.Descriptors)
        {
            Assert.True(
                BuiltInFeatureCatalog.Default.TryGet(descriptor.Id, out var canonical),
                $"Native feature '{descriptor.Id.Value}' must resolve from the catalog without plugin registration.");
            Assert.Same(descriptor, canonical);
        }
    }

    private sealed class SuccessfulCommand : IPluginCommand
    {
        public int TerminateCount { get; private set; }

        public Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            return Task.FromResult(PluginExecutionResult.Success());
        }

        public Task TerminateAsync(CancellationToken cancellationToken)
        {
            TerminateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TextViewFactory(string text) : IPluginViewFactory
    {
        public Control CreateView() =>
            new TextBlock { Text = text };
    }

    private sealed class RecordingHost : IPluginHost
    {
        public Task TerminatePluginAsync(string pluginId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
