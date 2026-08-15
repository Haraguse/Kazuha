using Avalonia.Controls;
using Avalonia.Threading;
using Luminalium.App.Features;

namespace Luminalium.App.Services;

public sealed class AvaloniaBuiltInFeatureDispatcher : IBuiltInFeatureDispatcher
{
    public Task<T> InvokeAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return Dispatcher.UIThread.InvokeAsync(operation);
    }
}

public sealed class AvaloniaBuiltInFeatureFactory : IBuiltInFeatureFactory
{
    private readonly Func<Window> _createWindow;
    private readonly Func<Window> _ownerProvider;

    public AvaloniaBuiltInFeatureFactory(Func<Window> ownerProvider, Func<Window> createWindow)
    {
        _ownerProvider = ownerProvider ?? throw new ArgumentNullException(nameof(ownerProvider));
        _createWindow = createWindow ?? throw new ArgumentNullException(nameof(createWindow));
    }

    public Task<IBuiltInFeatureInstance> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IBuiltInFeatureInstance>(new WindowFeatureInstance(_ownerProvider(), _createWindow()));
    }

    private sealed class WindowFeatureInstance(Window owner, Window window) : IBuiltInFeatureInstance
    {
        private int _closed;

        public Task ActivateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!window.IsVisible)
            {
                window.Show(owner);
            }
            window.Activate();
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _closed, 1) == 0 && window.IsVisible)
            {
                window.Close();
            }
            return Task.CompletedTask;
        }
    }
}
