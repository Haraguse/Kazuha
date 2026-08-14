using System.IO;
using System.Text;
using System.Text.Json;
using Windows.Media.Control;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

internal static class Program
{
    private const int MaxArtworkBytes = 8 * 1024 * 1024;

    public static async Task Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var lastSource = "";
        var lastArtworkKey = "";
        var lastArtworkDataUrl = "";

        while (true)
        {
            var requestId = Console.ReadLine();
            if (requestId is null || requestId == "__EXIT__")
            {
                break;
            }

            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var snapshot = await ReadBestSessionAsync(
                    manager, lastSource, lastArtworkKey, lastArtworkDataUrl
                );
                if (snapshot is not null)
                {
                    if (!string.IsNullOrWhiteSpace(snapshot.Source))
                    {
                        lastSource = snapshot.Source;
                    }
                    lastArtworkKey = snapshot.ArtworkKey ?? "";
                    if (!string.IsNullOrWhiteSpace(snapshot.ArtworkDataUrl))
                    {
                        lastArtworkDataUrl = snapshot.ArtworkDataUrl;
                    }
                }
                WriteJson(new
                {
                    request_id = requestId,
                    status = snapshot?.Status ?? "",
                    title = snapshot?.Title ?? "",
                    artist = snapshot?.Artist ?? "",
                    source = snapshot?.Source ?? "",
                    position_ms = snapshot?.PositionMs ?? 0,
                    duration_ms = snapshot?.DurationMs ?? 0,
                    artwork_key = snapshot?.ArtworkKey ?? "",
                    artwork_data_url = snapshot?.ArtworkDataUrl ?? ""
                });
            }
            catch (Exception ex)
            {
                lastSource = "";
                lastArtworkKey = "";
                lastArtworkDataUrl = "";
                Console.Error.WriteLine($"[SmtcHelper] Main loop error: {ex.Message}");
                WriteJson(new
                {
                    request_id = requestId,
                    status = "Stopped",
                    title = "",
                    artist = "",
                    source = "",
                    position_ms = 0,
                    duration_ms = 0,
                    artwork_key = "",
                    artwork_data_url = ""
                });
            }
        }
    }

    private static async Task<SessionSnapshot?> ReadBestSessionAsync(
        GlobalSystemMediaTransportControlsSessionManager manager,
        string lastSource,
        string lastArtworkKey,
        string lastArtworkDataUrl
    )
    {
        var currentSource = manager.GetCurrentSession()?.SourceAppUserModelId ?? "";
        var sessions = manager.GetSessions();
        var candidates = new List<SessionCandidate>();

        foreach (var session in sessions)
        {
            try
            {
                var props = await session.TryGetMediaPropertiesAsync();
                var playback = session.GetPlaybackInfo();
                var timeline = session.GetTimelineProperties();
                var source = session.SourceAppUserModelId ?? "";
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
                string artworkContentType = "";
                if (props?.Thumbnail is not null)
                {
                    var thumbnailPayload = await ReadThumbnailAsync(props.Thumbnail);
                    artworkBytes = thumbnailPayload.Bytes;
                    artworkContentType = thumbnailPayload.ContentType;
                }

                var title = FirstNonEmpty(
                    props?.Title,
                    props?.Subtitle,
                    props?.AlbumTitle,
                    GetFriendlySourceName(source)
                );
                var artist = FirstNonEmpty(
                    props?.Artist,
                    props?.AlbumArtist,
                    GetFirstGenre(props?.Genres)
                );
                var album = props?.AlbumTitle ?? "";
                var subtitle = props?.Subtitle ?? "";
                var albumArtist = props?.AlbumArtist ?? "";
                var playbackType = props?.PlaybackType.ToString() ?? "";
                var metadataScore = GetMetadataScore(
                    title,
                    artist,
                    album,
                    subtitle,
                    albumArtist,
                    artworkBytes
                );

                candidates.Add(new SessionCandidate(
                    source,
                    playback?.PlaybackStatus.ToString() ?? "",
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
                    source == currentSource,
                    string.Equals(source, lastSource, StringComparison.OrdinalIgnoreCase),
                    metadataScore
                ));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SmtcHelper] Session read error: {ex.Message}");
            }
        }

        var best = candidates
            .OrderBy(snapshot => RankStatus(snapshot.Status))
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

        var artworkKey = BuildArtworkKey(best);
        var artworkDataUrl = "";
        var artworkChanged = !string.Equals(artworkKey, lastArtworkKey, StringComparison.Ordinal);
        var needArtworkRetry = string.IsNullOrWhiteSpace(lastArtworkDataUrl);
        if (artworkChanged || needArtworkRetry)
        {
            artworkDataUrl = BuildDataUrl(best.ArtworkBytes, best.ArtworkContentType);
            if (!string.IsNullOrEmpty(artworkDataUrl))
            {
                Console.Error.WriteLine($"[SmtcHelper] Artwork loaded: {artworkKey} ({best.ArtworkBytes?.Length ?? 0} bytes, {best.ArtworkContentType})");
            }
            else
            {
                Console.Error.WriteLine($"[SmtcHelper] No artwork available for: {artworkKey}");
            }
        }

        return new SessionSnapshot(
            best.Source,
            best.Status,
            best.Title,
            best.Artist,
            best.PositionMs,
            best.DurationMs,
            artworkKey,
            artworkDataUrl,
            best.IsCurrent
        );
    }

    private static int RankStatus(string status) => status switch
    {
        "Playing" => 0,
        "Paused" => 1,
        "Changing" => 2,
        "Stopped" => 3,
        _ => 4,
    };

    private static string BuildArtworkKey(SessionCandidate candidate)
    {
        var source = candidate.Source ?? "";
        var title = candidate.Title ?? "";
        var artist = candidate.Artist ?? "";
        var album = candidate.Album ?? "";
        return $"{source}|{title}|{artist}|{album}";
    }

    private static async Task<ThumbnailPayload> ReadThumbnailAsync(IRandomAccessStreamReference thumbnail)
    {
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream is null)
            {
                Console.Error.WriteLine("[SmtcHelper] Thumbnail stream is null");
                return ThumbnailPayload.Empty;
            }

            var contentType = NormalizeContentType(stream.ContentType);
            using var classicStream = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await classicStream.CopyToAsync(ms);
            var buffer = ms.ToArray();

            if (buffer.Length > MaxArtworkBytes)
            {
                Console.Error.WriteLine($"[SmtcHelper] Thumbnail too large: {buffer.Length} bytes");
                return ThumbnailPayload.Empty;
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = DetectContentType(buffer);
            }

            return await EnsureDisplaySafeThumbnailAsync(buffer, contentType);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SmtcHelper] ReadThumbnailAsync failed: {ex.GetType().Name}: {ex.Message}");
            return ThumbnailPayload.Empty;
        }
    }

    private static string BuildDataUrl(byte[]? bytes, string contentType)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return "";
        }
        contentType = NormalizeContentType(contentType);
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            var detected = DetectContentType(bytes);
            if (!string.IsNullOrWhiteSpace(detected))
            {
                contentType = detected;
            }
        }
        contentType = NormalizeContentType(contentType);
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            return "";
        }
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static async Task<ThumbnailPayload> EnsureDisplaySafeThumbnailAsync(
        byte[] bytes,
        string contentType
    )
    {
        contentType = NormalizeContentType(contentType);
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
            var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied
            );

            using var output = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            encoder.SetSoftwareBitmap(bitmap);
            encoder.IsThumbnailGenerated = false;
            await encoder.FlushAsync();
            output.Seek(0);

            using var classicStream = output.AsStreamForRead();
            using var ms = new MemoryStream();
            await classicStream.CopyToAsync(ms);
            var converted = ms.ToArray();
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
        _ => false
    };

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "";
        }

        var normalized = contentType.Trim();
        var separatorIndex = normalized.IndexOf(';');
        if (separatorIndex >= 0)
        {
            normalized = normalized[..separatorIndex];
        }

        normalized = normalized.Trim().ToLowerInvariant();
        return normalized switch
        {
            "application/octet-stream" => "",
            "image/jpg" => "image/jpeg",
            "image/jfif" => "image/jpeg",
            "image/pjpeg" => "image/jpeg",
            "image/x-png" => "image/png",
            "image/x-ms-bmp" => "image/bmp",
            "image/vnd.microsoft.icon" => "image/x-icon",
            "image/svg" => "image/svg+xml",
            _ => normalized
        };
    }

    private static string DetectContentType(byte[] bytes)
    {
        if (bytes.Length == 0) return "";
        if (LooksLikeSvg(bytes))
            return "image/svg+xml";
        if (bytes.Length < 4) return "";
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        if (bytes[0] == 0xFF && bytes[1] == 0xD8)
            return "image/jpeg";
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes.Length > 11 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return "image/webp";
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            return "image/bmp";
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
            return "image/gif";
        if (bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00)
            return "image/tiff";
        if (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A)
            return "image/tiff";
        if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x01 && bytes[3] == 0x00)
            return "image/x-icon";
        if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x02 && bytes[3] == 0x00)
            return "image/x-icon";
        if (bytes.Length > 11
            && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
        {
            if (bytes[8] == 0x61 && bytes[9] == 0x76 && bytes[10] == 0x69 && bytes[11] == 0x66)
                return "image/avif";
            if (bytes[8] == 0x68 && bytes[9] == 0x65 && bytes[10] == 0x69 && (bytes[11] == 0x63 || bytes[11] == 0x66 || bytes[11] == 0x78 || bytes[11] == 0x6D))
                return "image/heic";
            if (bytes[8] == 0x6D && bytes[9] == 0x69 && bytes[10] == 0x66 && bytes[11] == 0x31)
                return "image/heif";
        }
        if (bytes[0] == 0xFF && bytes[1] == 0x0A)
            return "image/jxl";
        if (bytes.Length > 11
            && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x0C
            && bytes[4] == 0x4A && bytes[5] == 0x58 && bytes[6] == 0x4C && bytes[7] == 0x20
            && bytes[8] == 0x0D && bytes[9] == 0x0A && bytes[10] == 0x87 && bytes[11] == 0x0A)
            return "image/jxl";
        return "";
    }

    private static bool LooksLikeSvg(byte[] bytes)
    {
        var prefixLength = Math.Min(bytes.Length, 512);
        var prefix = Encoding.UTF8.GetString(bytes, 0, prefixLength).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }

    private static string GetFirstGenre(IReadOnlyList<string>? genres)
    {
        if (genres is null)
        {
            return "";
        }

        foreach (var genre in genres)
        {
            if (!string.IsNullOrWhiteSpace(genre))
            {
                return genre.Trim();
            }
        }

        return "";
    }

    private static int GetMetadataScore(
        string title,
        string artist,
        string album,
        string subtitle,
        string albumArtist,
        byte[]? artworkBytes
    )
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(title)) score += 8;
        if (!string.IsNullOrWhiteSpace(artist)) score += 4;
        if (!string.IsNullOrWhiteSpace(album)) score += 2;
        if (!string.IsNullOrWhiteSpace(subtitle)) score += 2;
        if (!string.IsNullOrWhiteSpace(albumArtist)) score += 1;
        if (artworkBytes is not null && artworkBytes.Length > 0) score += 3;
        return score;
    }

    private static string GetFriendlySourceName(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "";
        }

        var normalized = source.Trim();
        var bangIndex = normalized.IndexOf('!');
        if (bangIndex > 0)
        {
            normalized = normalized[..bangIndex];
        }

        var slashIndex = normalized.LastIndexOf('\\');
        if (slashIndex >= 0 && slashIndex < normalized.Length - 1)
        {
            normalized = normalized[(slashIndex + 1)..];
        }

        return normalized switch
        {
            "msedge.exe" => "Microsoft Edge",
            "chrome.exe" => "Google Chrome",
            "firefox.exe" => "Mozilla Firefox",
            "ApplicationFrameHost.exe" => "Application Frame Host",
            _ => normalized
        };
    }

    private static void WriteJson(object payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload));
        Console.Out.Flush();
    }
}

internal sealed record SessionSnapshot(
    string Source,
    string Status,
    string Title,
    string Artist,
    long PositionMs,
    long DurationMs,
    string ArtworkKey,
    string ArtworkDataUrl,
    bool IsCurrent
);

internal sealed record SessionCandidate(
    string Source,
    string Status,
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
    int MetadataScore
);

internal sealed record ThumbnailPayload(
    byte[]? Bytes,
    string ContentType
)
{
    public static ThumbnailPayload Empty { get; } = new(null, "");
}
