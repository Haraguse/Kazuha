using System.Globalization;

namespace Luminalium.Theming;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor FromHex(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);

        var normalized = hex[0] == '#'
            ? hex[1..]
            : hex;
        if (normalized.Length != 6)
        {
            throw new ArgumentException("RGB hex colors must contain exactly six hexadecimal digits.", nameof(hex));
        }

        return new RgbColor(
            byte.Parse(normalized[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    public HslColor ToHsl()
    {
        var red = R / 255d;
        var green = G / 255d;
        var blue = B / 255d;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var lightness = (max + min) / 2d;

        var hue = 0d;
        var saturation = 0d;
        var delta = max - min;
        if (delta > 0d)
        {
            saturation = lightness > 0.5d
                ? delta / (2d - max - min)
                : delta / (max + min);

            if (Math.Abs(max - red) < double.Epsilon)
            {
                hue = (green - blue) / delta + (green < blue ? 6d : 0d);
            }
            else if (Math.Abs(max - green) < double.Epsilon)
            {
                hue = (blue - red) / delta + 2d;
            }
            else
            {
                hue = (red - green) / delta + 4d;
            }

            hue *= 60d;
        }

        return new HslColor(
            ClampToInt(hue, 0, 359),
            ClampToInt(saturation * 255d, 0, 255),
            ClampToInt(lightness * 255d, 0, 255));
    }

    public string ToHexString() => $"#{R:X2}{G:X2}{B:X2}";

    public override string ToString() => ToHexString();

    private static int ClampToInt(double value, int min, int max) =>
        Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), min, max);
}
