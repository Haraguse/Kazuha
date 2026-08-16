namespace Luminalium.App.Services;

public enum RetryCloseDialogResult
{
    Retried,
    Closed,
    Cancelled,
    OwnerNotReady,
    AlreadyOpen,
    Failed,
}

public sealed record RetryCloseDialogRequest(
    string Title,
    string Message,
    Func<Task<bool>> RetryAsync,
    Action Close);

public sealed class StartupSplashLifecycle<T>
    where T : class
{
    private readonly Func<T> _create;
    private readonly Action<T> _show;
    private readonly Action<T> _close;
    private readonly Action _shutdown;
    private T? _current;
    private int _terminalCloseCalled;

    public StartupSplashLifecycle(Func<T> create, Action<T> show, Action<T> close, Action? shutdown = null)
    {
        _create = create;
        _show = show;
        _close = close;
        _shutdown = shutdown ?? (() => { });
    }

    public bool TryShowFresh()
    {
        CloseCurrent();
        var candidate = _create();
        try
        {
            _show(candidate);
            _current = candidate;
            return true;
        }
        catch
        {
            _close(candidate);
            return false;
        }
    }

    public bool Start() => TryShowFresh();

    public async Task<bool> RetryAsync(Func<Task<bool>> operation)
    {
        if (!TryShowFresh())
        {
            return false;
        }

        var succeeded = await operation().ConfigureAwait(true);
        if (succeeded)
        {
            CloseCurrent();
        }

        return succeeded;
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref _terminalCloseCalled, 1) == 0)
        {
            Dismiss();
            _shutdown();
        }
    }

    public void Dismiss() => CloseCurrent();

    private void CloseCurrent()
    {
        if (_current is not null)
        {
            var current = _current;
            _current = null;
            _close(current);
        }
    }
}
