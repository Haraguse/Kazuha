using Luminalium.App.Features;

namespace Luminalium.App.Services;

public sealed class BuiltInFeatureHost : IBuiltInFeatureHost
{
    private readonly BuiltInFeatureCatalog _catalog;
    private readonly IBuiltInFeatureDispatcher _dispatcher;
    private readonly IReadOnlyDictionary<BuiltInFeatureId, IBuiltInFeatureFactory> _factories;
    private readonly Dictionary<BuiltInFeatureId, SemaphoreSlim> _gates;
    private readonly Dictionary<BuiltInFeatureId, IBuiltInFeatureInstance> _shared = [];
    private readonly HashSet<IBuiltInFeatureInstance> _instances = [];
    private readonly object _sync = new();
    private bool _shuttingDown;
    private bool _disposed;

    public BuiltInFeatureHost(BuiltInFeatureCatalog catalog, IBuiltInFeatureDispatcher dispatcher, IReadOnlyDictionary<BuiltInFeatureId, IBuiltInFeatureFactory> factories)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _factories = factories ?? throw new ArgumentNullException(nameof(factories));
        _gates = catalog.Descriptors.ToDictionary(item => item.Id, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<BuiltInFeatureActivationResult> ActivateAsync(BuiltInFeatureId featureId, string? sourceRoute = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var route = sourceRoute ?? featureId.Value ?? string.Empty;
        if (!_catalog.TryGet(featureId, out var descriptor) || !_factories.TryGetValue(featureId, out var factory))
        {
            return Failure(BuiltInFeatureActivationErrorCode.NotFound, route, null, correlationId);
        }

        if (IsShuttingDown())
        {
            return Failure(BuiltInFeatureActivationErrorCode.ShutdownInProgress, route, featureId, correlationId);
        }

        if (!descriptor.IsAvailable())
        {
            return Failure(BuiltInFeatureActivationErrorCode.Unavailable, route, featureId, correlationId);
        }

        var gate = _gates[featureId];
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsShuttingDown())
            {
                return Failure(BuiltInFeatureActivationErrorCode.ShutdownInProgress, route, featureId, correlationId);
            }

            if (descriptor.ActivationMode != BuiltInFeatureActivationMode.FreshWindow && TryGetShared(featureId, out var existing))
            {
                await DispatchAsync(() => existing.ActivateAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
                return BuiltInFeatureActivationResult.Success(featureId, route, correlationId, BuiltInFeatureActivationDisposition.Coalesced);
            }

            try
            {
                var instance = await _dispatcher.InvokeAsync(async () =>
                {
                    var created = await factory.CreateAsync(cancellationToken).ConfigureAwait(true);
                    await created.ActivateAsync(cancellationToken).ConfigureAwait(true);
                    return created;
                }, cancellationToken).ConfigureAwait(false);
                if (!TryTrack(featureId, descriptor.ActivationMode, instance))
                {
                    await DispatchAsync(() => instance.CloseAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
                    return Failure(BuiltInFeatureActivationErrorCode.ShutdownInProgress, route, featureId, correlationId);
                }

                return BuiltInFeatureActivationResult.Success(featureId, route, correlationId);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return Failure(BuiltInFeatureActivationErrorCode.InitializationFailed, route, featureId, correlationId);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task BeginShutdownAsync(CancellationToken cancellationToken = default)
    {
        IBuiltInFeatureInstance[] instances;
        lock (_sync)
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            instances = _instances.ToArray();
            _instances.Clear();
            _shared.Clear();
        }

        foreach (var instance in instances)
        {
            await DispatchAsync(() => instance.CloseAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await BeginShutdownAsync().ConfigureAwait(false);
        foreach (var gate in _gates.Values)
        {
            gate.Dispose();
        }
    }

    private bool IsShuttingDown()
    {
        lock (_sync)
        {
            return _shuttingDown;
        }
    }

    private bool TryGetShared(BuiltInFeatureId id, out IBuiltInFeatureInstance instance)
    {
        lock (_sync)
        {
            return _shared.TryGetValue(id, out instance!);
        }
    }

    private bool TryTrack(BuiltInFeatureId id, BuiltInFeatureActivationMode mode, IBuiltInFeatureInstance instance)
    {
        lock (_sync)
        {
            if (_shuttingDown)
            {
                return false;
            }

            _instances.Add(instance);
            if (mode != BuiltInFeatureActivationMode.FreshWindow)
            {
                _shared[id] = instance;
            }

            return true;
        }
    }

    private Task<bool> DispatchAsync(Func<Task> operation, CancellationToken cancellationToken) =>
        _dispatcher.InvokeAsync(async () =>
        {
            await operation().ConfigureAwait(true);
            return true;
        }, cancellationToken);

    private static BuiltInFeatureActivationResult Failure(BuiltInFeatureActivationErrorCode code, string route, BuiltInFeatureId? id, string? correlationId) =>
        BuiltInFeatureActivationResult.Failure(code, route, id, correlationId);
}
