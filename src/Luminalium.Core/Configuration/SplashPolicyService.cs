using System.Globalization;

namespace Luminalium.Core.Configuration;

public sealed class SplashPolicyService
{
    public const string DefaultStyle = "default";

    private readonly string[] _supportedStyles =
    [
        DefaultStyle,
        "nina_iseri_1_2",
    ];

    public SplashPolicyResult Evaluate(
        GeneralSettings settings,
        bool isAutoStart,
        TimeOnly currentTime)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Evaluate(
            settings.SplashMode,
            settings.SplashStyle,
            settings.ShowDetailedSplash,
            settings.SplashStartTime,
            settings.SplashEndTime,
            isAutoStart,
            currentTime);
    }

    public SplashPolicyResult Evaluate(
        SplashMode mode,
        string? style,
        bool showDetailedSplash,
        string? startTime,
        string? endTime,
        bool isAutoStart,
        TimeOnly currentTime)
    {
        var isVisible = mode switch
        {
            SplashMode.Always => true,
            SplashMode.Never => false,
            SplashMode.HideOnAutoStart => !isAutoStart,
            SplashMode.TimeRange => IsWithinTimeRange(startTime, endTime, currentTime),
            _ => false,
        };

        return new SplashPolicyResult(
            isVisible,
            ResolveStyle(style),
            isVisible && showDetailedSplash);
    }

    private string ResolveStyle(string? style)
    {
        var normalizedStyle = style?.Trim();
        return _supportedStyles.FirstOrDefault(
                   supportedStyle => string.Equals(
                       supportedStyle,
                       normalizedStyle,
                       StringComparison.OrdinalIgnoreCase))
               ?? DefaultStyle;
    }

    private static bool IsWithinTimeRange(
        string? startTime,
        string? endTime,
        TimeOnly currentTime)
    {
        if (!TryParseTime(startTime, out var start) ||
            !TryParseTime(endTime, out var end))
        {
            return true;
        }

        if (start == end)
        {
            return true;
        }

        if (start < end)
        {
            return currentTime >= start && currentTime <= end;
        }

        return currentTime >= start || currentTime <= end;
    }

    private static bool TryParseTime(string? value, out TimeOnly time)
    {
        return TimeOnly.TryParseExact(
            value?.Trim(),
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
    }
}

public sealed record SplashPolicyResult(
    bool IsVisible,
    string Style,
    bool ShowDetailedSplash);
