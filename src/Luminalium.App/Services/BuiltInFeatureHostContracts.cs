using Luminalium.App.Features;

namespace Luminalium.App.Services;

public interface IBuiltInFeatureDispatcher
{
    Task<T> InvokeAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
}

public interface IBuiltInFeatureInstance
{
    Task ActivateAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}

public interface IBuiltInFeatureFactory
{
    Task<IBuiltInFeatureInstance> CreateAsync(CancellationToken cancellationToken);
}

public interface IBuiltInFeatureHost : IAsyncDisposable
{
    Task<BuiltInFeatureActivationResult> ActivateAsync(
        BuiltInFeatureId featureId,
        string? sourceRoute = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task BeginShutdownAsync(CancellationToken cancellationToken = default);
}
