using Luminalium.Presentation;

namespace Luminalium.App.Services;

/// <summary>
/// Polls for a slideshow window and invokes <c>openOverlay</c> once per
/// slideshow session (inactive -> active transition). The poll loop runs as an
/// async task; continuations keep the captured context so the open callback is
/// raised on the UI thread in the Avalonia app. A single failed poll never
/// terminates the loop.
/// </summary>
public sealed class SlideshowWatcher : IDisposable
{
    private readonly ISlideshowWindowAdapter _windowAdapter;
    private readonly Action _openOverlay;
    private readonly PresentationHostKind _watchKind;
    private readonly TimeSpan _pollInterval;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _wasActive;
    private bool _disposed;

    public SlideshowWatcher(
        ISlideshowWindowAdapter windowAdapter,
        Action openOverlay,
        PresentationHostKind watchKind = PresentationHostKind.PowerPoint,
        TimeSpan? pollInterval = null)
    {
        ArgumentNullException.ThrowIfNull(windowAdapter);
        ArgumentNullException.ThrowIfNull(openOverlay);
        _windowAdapter = windowAdapter;
        _openOverlay = openOverlay;
        _watchKind = watchKind;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(1200);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_loop is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = RunLoopAsync(_cts.Token);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _loop = null;
        GC.SuppressFinalize(this);
    }

    internal Task PollOnceAsync(CancellationToken cancellationToken = default) =>
        PollAsync(cancellationToken);

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed poll must never terminate the watcher loop.
            }

            try
            {
                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var result = await _windowAdapter.FindSlideshowWindowAsync(_watchKind, cancellationToken).ConfigureAwait(true);
        var isActive = result.IsSuccess && result.Value is not null;
        if (isActive && !_wasActive)
        {
            _wasActive = true;
            _openOverlay();
            return;
        }

        if (!isActive)
        {
            _wasActive = false;
        }
    }
}