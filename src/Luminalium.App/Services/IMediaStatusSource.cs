using Luminalium.Core.Media;

namespace Luminalium.App.Services;

/// <summary>
/// Read-only media status surface for the status bar plugin. Wraps the Task 9
/// SMTC snapshot contract behind an injectable interface so headless tests can
/// substitute a fake session (title/artist).
/// </summary>
public interface IMediaStatusSource
{
    Task<MediaStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record MediaStatusSnapshot(string Title, string Artist, bool IsCurrent)
{
    public static MediaStatusSnapshot Empty { get; } = new(string.Empty, string.Empty, false);
}

/// <summary>
/// Real status source backed by <see cref="ISmtcService"/>. Never throws: an
/// empty/unavailable session degrades to an empty snapshot so the status bar
/// always renders its localized "no media" placeholder.
/// </summary>
public sealed class MediaStatusSource : IMediaStatusSource
{
    private readonly ISmtcService _smtcService;

    public MediaStatusSource(ISmtcService smtcService)
    {
        _smtcService = smtcService ?? throw new ArgumentNullException(nameof(smtcService));
    }

    public async Task<MediaStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _smtcService.TryGetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return MediaStatusSnapshot.Empty;
            }

            var snapshot = result.Value;
            if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.Title))
            {
                return MediaStatusSnapshot.Empty;
            }

            return new MediaStatusSnapshot(snapshot.Title, snapshot.Artist, snapshot.IsCurrent);
        }
        catch (Exception)
        {
            return MediaStatusSnapshot.Empty;
        }
    }
}
