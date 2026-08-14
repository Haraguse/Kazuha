namespace Luminalium.Plugins;

public sealed class BuiltInPluginRegistry
{
    private readonly Dictionary<string, BuiltInPlugin> _pluginsById = new(StringComparer.Ordinal);
    private readonly List<BuiltInPlugin> _plugins = [];

    public int Count => _plugins.Count;

    public PluginRegistryResult Register(BuiltInPlugin? plugin)
    {
        if (plugin is null)
        {
            return PluginRegistryResult.Failure(new PluginRegistrationValidationError(
                "Built-in plugin registration requires a plugin instance."));
        }

        var id = plugin.Metadata.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return PluginRegistryResult.Failure(new PluginRegistrationValidationError(
                "Built-in plugin registration requires a non-empty id.",
                id));
        }

        if (!_pluginsById.TryAdd(id, plugin))
        {
            return PluginRegistryResult.Failure(new DuplicateRegistrationError(id));
        }

        _plugins.Add(plugin);
        return PluginRegistryResult.Success();
    }

    public BuiltInPlugin? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _pluginsById.TryGetValue(id.Trim(), out var plugin) ? plugin : null;
    }

    public IReadOnlyList<BuiltInPlugin> Enumerate() =>
        _plugins.ToArray();

    public async Task<PluginRegistryResult> TerminateAllAsync(CancellationToken cancellationToken = default)
    {
        var failures = new List<PluginTerminationFailure>();

        foreach (var plugin in _plugins)
        {
            try
            {
                await plugin.TerminateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                failures.Add(new PluginTerminationFailure(
                    plugin.Metadata.Id,
                    $"Built-in plugin '{plugin.Metadata.Id}' failed to terminate.",
                    exc.Message));
            }
        }

        return failures.Count == 0
            ? PluginRegistryResult.Success()
            : PluginRegistryResult.Failure(new PluginTerminationAggregateError(failures));
    }
}
