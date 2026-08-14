namespace Luminalium.App.Services;

/// <summary>
/// Injectable time source used by the timer plugin so that countdown
/// behavior can be driven deterministically in headless tests.
/// </summary>
public interface IClockService
{
    DateTimeOffset UtcNow { get; }

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>
/// Real clock used by the app shell. Never referenced from tests; tests
/// substitute a deterministic fake clock instead.
/// </summary>
public sealed class SystemClockService : IClockService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
