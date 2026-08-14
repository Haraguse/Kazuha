namespace Luminalium.Core.Platform;

public readonly record struct DisplayPoint(double X, double Y);

public readonly record struct DisplayRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

public sealed record MonitorInfo(
    string DeviceName,
    DisplayRect Bounds,
    double ScaleFactor,
    bool IsPrimary);

public static class DisplayMath
{
    public static DisplayPoint PhysicalToLogical(DisplayPoint point, double scaleFactor) =>
        new(point.X / RequirePositiveScaleFactor(scaleFactor), point.Y / scaleFactor);

    public static DisplayPoint LogicalToPhysical(DisplayPoint point, double scaleFactor) =>
        new(point.X * RequirePositiveScaleFactor(scaleFactor), point.Y * scaleFactor);

    public static DisplayRect PhysicalToLogical(DisplayRect rect, double scaleFactor) =>
        ScaleRect(rect, 1 / RequirePositiveScaleFactor(scaleFactor));

    public static DisplayRect LogicalToPhysical(DisplayRect rect, double scaleFactor) =>
        ScaleRect(rect, RequirePositiveScaleFactor(scaleFactor));

    private static DisplayRect ScaleRect(DisplayRect rect, double factor) =>
        new(
            Round(rect.Left * factor),
            Round(rect.Top * factor),
            Round(rect.Right * factor),
            Round(rect.Bottom * factor));

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static double RequirePositiveScaleFactor(double scaleFactor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scaleFactor);
        return scaleFactor;
    }
}
