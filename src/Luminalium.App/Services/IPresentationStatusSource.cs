using Luminalium.Presentation;

namespace Luminalium.App.Services;

/// <summary>
/// Read-only presentation status surface for the status bar plugin. Wraps the
/// Task 15 <see cref="PresentationMonitor"/> behind an injectable contract so
/// headless tests can substitute a fake (e.g. "slide 1 / 5").
/// </summary>
public interface IPresentationStatusSource
{
    Task<PresentationStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record PresentationStatusSnapshot(int CurrentSlide, int SlideCount, bool IsSlideShow)
{
    public static PresentationStatusSnapshot Empty { get; } = new(0, 0, false);
}

/// <summary>
/// Real status source backed by a <see cref="PresentationMonitor"/>. Never
/// throws: any monitor failure degrades to an empty snapshot so the status bar
/// always renders its localized "no presentation" placeholder.
/// </summary>
public sealed class PresentationStatusSource : IPresentationStatusSource
{
    private readonly PresentationMonitor _monitor;

    public PresentationStatusSource(PresentationMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    public async Task<PresentationStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitor.GetStateAsync(cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return PresentationStatusSnapshot.Empty;
            }

            var state = result.Value;
            return new PresentationStatusSnapshot(
                state.CurrentSlide,
                state.SlideCount,
                state.IsSlideShow);
        }
        catch (Exception)
        {
            return PresentationStatusSnapshot.Empty;
        }
    }
}
