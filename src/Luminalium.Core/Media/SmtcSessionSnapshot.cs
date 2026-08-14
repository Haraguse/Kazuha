namespace Luminalium.Core.Media;

public sealed record SmtcSessionSnapshot(
    string SourceAppUserModelId,
    SmtcPlaybackStatus PlaybackStatus,
    string Title,
    string Artist,
    long PositionMs,
    long DurationMs,
    string ArtworkKey,
    string ArtworkDataUrl,
    bool IsCurrent)
{
    public static SmtcSessionSnapshot Empty { get; } = new(
        string.Empty,
        SmtcPlaybackStatus.Stopped,
        string.Empty,
        string.Empty,
        0,
        0,
        string.Empty,
        string.Empty,
        false);
}
