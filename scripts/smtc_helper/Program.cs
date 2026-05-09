using System.IO;
using System.Text;
using System.Text.Json;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

internal static class Program
{
    private const int MaxArtworkBytes = 4 * 1024 * 1024;

    public static async Task Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

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
                    manager, lastArtworkKey, lastArtworkDataUrl
                );
                if (snapshot is not null)
                {
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
                    artworkBytes = await ReadThumbnailBytesAsync(props.Thumbnail);
                    if (artworkBytes is not null && artworkBytes.Length > 0)
                    {
                        artworkContentType = await GetThumbnailContentTypeAsync(props.Thumbnail);
                    }
                }

                candidates.Add(new SessionCandidate(
                    session.SourceAppUserModelId ?? "",
                    playback?.PlaybackStatus.ToString() ?? "",
                    props?.Title ?? "",
                    props?.Artist ?? "",
                    props?.AlbumTitle ?? "",
                    (long)position.TotalMilliseconds,
                    (long)duration.TotalMilliseconds,
                    artworkBytes,
                    artworkContentType,
                    session.SourceAppUserModelId == currentSource
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

    private static async Task<byte[]?> ReadThumbnailBytesAsync(IRandomAccessStreamReference thumbnail)
    {
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream is null)
            {
                Console.Error.WriteLine("[SmtcHelper] Thumbnail stream is null");
                return null;
            }
            
            using var classicStream = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await classicStream.CopyToAsync(ms);
            var buffer = ms.ToArray();
            
            if (buffer.Length > MaxArtworkBytes)
            {
                Console.Error.WriteLine($"[SmtcHelper] Thumbnail too large: {buffer.Length} bytes");
                return null;
            }
            
            return buffer;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SmtcHelper] ReadThumbnailBytesAsync failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> GetThumbnailContentTypeAsync(IRandomAccessStreamReference thumbnail)
    {
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream is not null && !string.IsNullOrWhiteSpace(stream.ContentType))
            {
                return stream.ContentType;
            }
        }
        catch { }
        return "";
    }

    private static string BuildDataUrl(byte[]? bytes, string contentType)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return "";
        }
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
        {
            var detected = DetectContentType(bytes);
            if (!string.IsNullOrWhiteSpace(detected))
            {
                contentType = detected;
            }
        }
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
        {
            return "";
        }
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string DetectContentType(byte[] bytes)
    {
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
        if (bytes.Length > 11
            && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
        {
            if (bytes[8] == 0x61 && bytes[9] == 0x76 && bytes[10] == 0x69 && bytes[11] == 0x66)
                return "image/avif";
            if (bytes[8] == 0x68 && bytes[9] == 0x65 && bytes[10] == 0x69 && (bytes[11] == 0x63 || bytes[11] == 0x66 || bytes[11] == 0x78 || bytes[11] == 0x6D))
                return "image/heic";
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
    long PositionMs,
    long DurationMs,
    byte[]? ArtworkBytes,
    string ArtworkContentType,
    bool IsCurrent
);
