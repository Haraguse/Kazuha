using Luminalium.App.Features;
using Luminalium.Plugins;

namespace Luminalium.App.Services;

/// <summary>
/// App-facing boundary for genuine external plugin objects. Keeps the native
/// built-in feature catalog authoritative: an external plugin whose ID
/// collides with a canonical built-in ID is rejected deterministically, so a
/// third-party extension can never shadow or hijack a native built-in.
/// <see cref="Luminalium.Plugins.BuiltInPluginRegistry"/> stays generic and
/// needs no knowledge of app types.
/// </summary>
public sealed class ExternalExtensionRegistry
{
    private readonly BuiltInPluginRegistry _inner;

    public ExternalExtensionRegistry(BuiltInPluginRegistry? inner = null)
    {
        _inner = inner ?? new BuiltInPluginRegistry();
    }

    public int Count => _inner.Count;

    public PluginRegistryResult Register(BuiltInPlugin? plugin)
    {
        if (plugin is not null && BuiltInFeatureId.TryParseCanonical(plugin.Metadata.Id, out _))
        {
            return PluginRegistryResult.Failure(new PluginRegistrationCollisionError(
                plugin.Metadata.Id,
                "a native built-in feature"));
        }

        return _inner.Register(plugin);
    }

    public BuiltInPlugin? Get(string id) => _inner.Get(id);

    public IReadOnlyList<BuiltInPlugin> Enumerate() => _inner.Enumerate();

    public Task<PluginRegistryResult> TerminateAllAsync(CancellationToken cancellationToken = default) =>
        _inner.TerminateAllAsync(cancellationToken);
}
