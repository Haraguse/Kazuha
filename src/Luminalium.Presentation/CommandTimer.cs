namespace Luminalium.Presentation;

public sealed class SlidingWindowCommandTimer : ICommandTimer
{
    private readonly object _gate = new();
    private readonly Queue<DateTimeOffset> _pageTurnTimes = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _window;
    private readonly int _maxPerWindow;

    public SlidingWindowCommandTimer(
        int maxPerWindow = 2,
        TimeSpan? window = null,
        TimeProvider? timeProvider = null)
    {
        _maxPerWindow = Math.Max(1, maxPerWindow);
        _window = window ?? TimeSpan.FromSeconds(1);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public PresentationOperationResult TryConsumePageTurn()
    {
        var now = _timeProvider.GetUtcNow();
        lock (_gate)
        {
            while (_pageTurnTimes.Count > 0 && now - _pageTurnTimes.Peek() > _window)
            {
                _pageTurnTimes.Dequeue();
            }

            if (_pageTurnTimes.Count >= _maxPerWindow)
            {
                return PresentationOperationResult.Failure(PresentationErrors.CommandRejected(
                    "The presentation page-turn command was throttled.",
                    $"Maximum {_maxPerWindow} page-turn command(s) per {_window.TotalSeconds:0.###} second(s)."));
            }

            _pageTurnTimes.Enqueue(now);
            return PresentationOperationResult.Success();
        }
    }
}
