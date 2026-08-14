namespace Luminalium.Plugins;

public interface IPluginHost
{
    /// <summary>
    /// Requests termination of a known built-in plugin without exposing the host application object.
    /// </summary>
    Task TerminatePluginAsync(string pluginId, CancellationToken cancellationToken);
}

/// <summary>
/// Bounded host context for built-in plugins.
/// </summary>
/// <remarks>
/// This intentionally exposes only cancellation and the narrow plugin host surface. Metadata and
/// commands can be consumed without views by future non-UI hosts, including remote-control surfaces,
/// without passing app/window objects or service locators into plugins.
/// </remarks>
public sealed record PluginContext
{
    public PluginContext(IPluginHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);

        Host = host;
        CancellationToken = cancellationToken;
    }

    public CancellationToken CancellationToken { get; }

    public IPluginHost Host { get; }
}
