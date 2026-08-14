namespace Luminalium.App.Overlay;

public readonly record struct StrokePoint(double X, double Y, double Pressure = 1.0);

public sealed record StrokeModel(string ColorHex, double Thickness, IReadOnlyList<StrokePoint> Points)
{
    public static StrokeModel Create(string colorHex, double thickness, IEnumerable<StrokePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return new StrokeModel(
            NormalizeColorHex(colorHex),
            Math.Max(1.0, thickness),
            points.ToArray());
    }

    public static string NormalizeColorHex(string colorHex)
    {
        var value = (colorHex ?? string.Empty).Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length != 6 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            return "#000000";
        }

        return "#" + value.ToUpperInvariant();
    }
}
