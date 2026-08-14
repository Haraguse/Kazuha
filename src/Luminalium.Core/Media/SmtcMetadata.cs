using System.Text;

namespace Luminalium.Core.Media;

public static class SmtcMetadata
{
    public static int RankStatus(SmtcPlaybackStatus status) => status switch
    {
        SmtcPlaybackStatus.Playing => 0,
        SmtcPlaybackStatus.Paused => 1,
        SmtcPlaybackStatus.Changing => 2,
        SmtcPlaybackStatus.Stopped => 3,
        _ => 4,
    };

    public static int GetMetadataScore(
        string? title,
        string? artist,
        string? album,
        string? subtitle,
        string? albumArtist,
        byte[]? artworkBytes)
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

    public static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    public static string GetFirstGenre(IReadOnlyList<string>? genres)
    {
        if (genres is null)
        {
            return string.Empty;
        }

        foreach (var genre in genres)
        {
            if (!string.IsNullOrWhiteSpace(genre))
            {
                return genre.Trim();
            }
        }

        return string.Empty;
    }

    public static string GetFriendlySourceName(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
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
            _ => normalized,
        };
    }

    public static string BuildArtworkKey(string? source, string? title, string? artist, string? album) =>
        $"{source ?? string.Empty}|{title ?? string.Empty}|{artist ?? string.Empty}|{album ?? string.Empty}";

    public static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
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
            "application/octet-stream" => string.Empty,
            "image/jpg" => "image/jpeg",
            "image/jfif" => "image/jpeg",
            "image/pjpeg" => "image/jpeg",
            "image/x-png" => "image/png",
            "image/x-ms-bmp" => "image/bmp",
            "image/vnd.microsoft.icon" => "image/x-icon",
            "image/svg" => "image/svg+xml",
            _ => normalized,
        };
    }

    public static string DetectContentType(byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;
        if (LooksLikeSvg(bytes)) return "image/svg+xml";
        if (bytes.Length < 4) return string.Empty;
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
        return string.Empty;
    }

    public static bool LooksLikeSvg(byte[] bytes)
    {
        var prefixLength = Math.Min(bytes.Length, 512);
        var prefix = Encoding.UTF8.GetString(bytes, 0, prefixLength).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
