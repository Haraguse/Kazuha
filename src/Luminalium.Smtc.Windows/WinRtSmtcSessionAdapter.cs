using System.Runtime.InteropServices.WindowsRuntime;
using Luminalium.Core.Media;
using Luminalium.Core.Platform;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Luminalium.Smtc.Windows;

public sealed class WinRtSmtcSessionAdapter : ISmtcSessionAdapter
{
    private const int MaxArtworkBytes = 8 * 1024 * 1024;

    private string _lastSource = string.Empty;
    private string _lastArtworkKey = string.Empty;
    private string _lastArtworkDataUrl = string.Empty;

    public async Task<PlatformOperationResult<SmtcSessionSnapshot>> TryGetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var snapshot = await ReadBestSessionAsync(
                manager,
                _lastSource,
                _lastArtworkKey,
                _lastArtworkDataUrl,
                cancellationToken);

            if (snapshot is null)
            {
                return PlatformOperation.Success(SmtcSessionSnapshot.Empty);
            }

            if (!string.IsNullOrWhiteSpace(snapshot.SourceAppUserModelId))
            {
                _lastSource = snapshot.SourceAppUserModelId;
            }

            _lastArtworkKey = snapshot.ArtworkKey;
            if (!string.IsNullOrWhiteSpace(snapshot.ArtworkDataUrl))
            {
                _lastArtworkDataUrl = snapshot.ArtworkDataUrl;
            }

            return PlatformOperation.Success(snapshot);
        }
        catch (Exception ex)
        {
            _lastSource = string.Empty;
            _lastArtworkKey = string.Empty;
            _lastArtworkDataUrl = string.Empty;
            return PlatformOperation.Failure<SmtcSessionSnapshot>(new PlatformOperationError(
                PlatformOperationErrorCode.Unavailable,
                "WinRT SMTC session enumeration is unavailable.",
                $"{ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static async Task<SmtcSessionSnapshot?> ReadBestSessionAsync(
        GlobalSystemMediaTransportControlsSessionManager manager,
        string lastSource,
        string lastArtworkKey,
        string lastArtworkDataUrl,
        CancellationToken cancellationToken)
    {
        var currentSource = manager.GetCurrentSession()?.SourceAppUserModelId ?? string.Empty;
        var sessions = manager.GetSessions();
        var candidates = new List<SessionCandidate>();

        foreach (var session in sessions)
        {
            try
            {
                var props = await session.TryGetMediaPropertiesAsync();
                var playback = session.GetPlaybackInfo();
                var timeline = session.GetTimelineProperties();
                var source = session.SourceAppUserModelId ?? string.Empty;
                var start = timeline.StartTime;
                var duration = timeline.EndTime - start;
                var position = timeline.Position - start;
                if (duration < TimeSpan.Zero)
                {
                    duration = TimeSpan.Zero;
                }

                if (position < TimeSpan.Zero)
                {
                    position = TimeSpan.Zero;
                }

                if (duration > TimeSpan.Zero && position > duration)
                {
                    position = duration;
                }

                byte[]? artworkBytes = null;
                var artworkContentType = string.Empty;
                if (props?.Thumbnail is not null)
                {
                    var thumbnailPayload = await ReadThumbnailAsync(props.Thumbnail, cancellationToken);
                    artworkBytes = thumbnailPayload.Bytes;
                    artworkContentType = thumbnailPayload.ContentType;
                }

                var title = SmtcMetadata.FirstNonEmpty(
                    props?.Title,
                    props?.Subtitle,
                    props?.AlbumTitle,
                    SmtcMetadata.GetFriendlySourceName(source));
                var artist = SmtcMetadata.FirstNonEmpty(
                    props?.Artist,
                    props?.AlbumArtist,
                    SmtcMetadata.GetFirstGenre(props?.Genres));
                var album = props?.AlbumTitle ?? string.Empty;
                var subtitle = props?.Subtitle ?? string.Empty;
                var albumArtist = props?.AlbumArtist ?? string.Empty;
                var playbackType = props?.PlaybackType.ToString() ?? string.Empty;
                var metadataScore = SmtcMetadata.GetMetadataScore(
                    title,
                    artist,
                    album,
                    subtitle,
                    albumArtist,
                    artworkBytes);

                candidates.Add(new SessionCandidate(
                    source,
                    MapPlaybackStatus(playback?.PlaybackStatus),
                    title,
                    artist,
                    album,
                    subtitle,
                    albumArtist,
                    playbackType,
                    (long)position.TotalMilliseconds,
                    (long)duration.TotalMilliseconds,
                    artworkBytes,
                    artworkContentType,
                    string.Equals(source, currentSource, StringComparison.Ordinal),
                    string.Equals(source, lastSource, StringComparison.OrdinalIgnoreCase),
                    metadataScore));
            }
            catch
            {
            }
        }

        var best = candidates
            .OrderBy(snapshot => SmtcMetadata.RankStatus(snapshot.PlaybackStatus))
            .ThenByDescending(snapshot => snapshot.IsCurrent)
            .ThenByDescending(snapshot => snapshot.IsLastSource)
            .ThenByDescending(snapshot => snapshot.MetadataScore)
            .ThenByDescending(snapshot => snapshot.ArtworkBytes is not null && snapshot.ArtworkBytes.Length > 0)
            .ThenByDescending(snapshot => snapshot.DurationMs > 0)
            .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.Title))
            .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.Artist))
            .FirstOrDefault();
        if (best is null)
        {
            return null;
        }

        var artworkKey = SmtcMetadata.BuildArtworkKey(best.Source, best.Title, best.Artist, best.Album);
        var artworkDataUrl = string.Empty;
        var artworkChanged = !string.Equals(artworkKey, lastArtworkKey, StringComparison.Ordinal);
        var needArtworkRetry = string.IsNullOrWhiteSpace(lastArtworkDataUrl);
        if (artworkChanged || needArtworkRetry)
        {
            artworkDataUrl = BuildDataUrl(best.ArtworkBytes, best.ArtworkContentType);
        }

        return new SmtcSessionSnapshot(
            best.Source,
            best.PlaybackStatus,
            best.Title,
            best.Artist,
            best.PositionMs,
            best.DurationMs,
            artworkKey,
            artworkDataUrl,
            best.IsCurrent);
    }

    private static async Task<ThumbnailPayload> ReadThumbnailAsync(
        IRandomAccessStreamReference thumbnail,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream is null)
            {
                return ThumbnailPayload.Empty;
            }

            var contentType = SmtcMetadata.NormalizeContentType(stream.ContentType);
            using var classicStream = stream.AsStreamForRead();
            var buffer = await ReadStreamWithCapAsync(classicStream, cancellationToken);
            if (buffer is null)
            {
                return ThumbnailPayload.Empty;
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = SmtcMetadata.DetectContentType(buffer);
            }

            return await EnsureDisplaySafeThumbnailAsync(buffer, contentType);
        }
        catch
        {
            return ThumbnailPayload.Empty;
        }
    }

    private static async Task<byte[]?> ReadStreamWithCapAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
            {
                return memoryStream.ToArray();
            }

            if (memoryStream.Length + bytesRead > MaxArtworkBytes)
            {
                return null;
            }

            memoryStream.Write(buffer, 0, bytesRead);
        }
    }

    private static string BuildDataUrl(byte[]? bytes, string contentType)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return string.Empty;
        }

        contentType = SmtcMetadata.NormalizeContentType(contentType);
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            var detected = SmtcMetadata.DetectContentType(bytes);
            if (!string.IsNullOrWhiteSpace(detected))
            {
                contentType = detected;
            }
        }

        contentType = SmtcMetadata.NormalizeContentType(contentType);
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static async Task<ThumbnailPayload> EnsureDisplaySafeThumbnailAsync(
        byte[] bytes,
        string contentType)
    {
        contentType = SmtcMetadata.NormalizeContentType(contentType);
        if (IsBrowserFriendlyContentType(contentType))
        {
            return new ThumbnailPayload(bytes, contentType);
        }

        var pngBytes = await TryConvertToPngAsync(bytes);
        if (pngBytes is not null && pngBytes.Length > 0)
        {
            return new ThumbnailPayload(pngBytes, "image/png");
        }

        return new ThumbnailPayload(bytes, contentType);
    }

    private static async Task<byte[]?> TryConvertToPngAsync(byte[] bytes)
    {
        try
        {
            using var input = new InMemoryRandomAccessStream();
            await input.WriteAsync(bytes.AsBuffer());
            input.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(input);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            using var output = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            encoder.SetSoftwareBitmap(bitmap);
            encoder.IsThumbnailGenerated = false;
            await encoder.FlushAsync();
            output.Seek(0);

            using var classicStream = output.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await classicStream.CopyToAsync(memoryStream);
            var converted = memoryStream.ToArray();
            if (converted.Length == 0 || converted.Length > MaxArtworkBytes)
            {
                return null;
            }

            return converted;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsBrowserFriendlyContentType(string contentType) => contentType switch
    {
        "image/png" => true,
        "image/jpeg" => true,
        "image/gif" => true,
        "image/webp" => true,
        "image/bmp" => true,
        "image/svg+xml" => true,
        _ => false,
    };

    private static SmtcPlaybackStatus MapPlaybackStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? playbackStatus) =>
        playbackStatus switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => SmtcPlaybackStatus.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => SmtcPlaybackStatus.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => SmtcPlaybackStatus.Changing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => SmtcPlaybackStatus.Stopped,
            _ => SmtcPlaybackStatus.Unknown,
        };

    private sealed record SessionCandidate(
        string Source,
        SmtcPlaybackStatus PlaybackStatus,
        string Title,
        string Artist,
        string Album,
        string Subtitle,
        string AlbumArtist,
        string PlaybackType,
        long PositionMs,
        long DurationMs,
        byte[]? ArtworkBytes,
        string ArtworkContentType,
        bool IsCurrent,
        bool IsLastSource,
        int MetadataScore);

    private sealed record ThumbnailPayload(byte[]? Bytes, string ContentType)
    {
        public static ThumbnailPayload Empty { get; } = new(null, string.Empty);
    }
}
