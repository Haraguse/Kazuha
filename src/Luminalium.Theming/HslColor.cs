namespace Luminalium.Theming;

public readonly record struct HslColor(int H, int S, int L)
{
    public RgbColor ToRgb()
    {
        var hue = NormalizeHue(H) / 360d;
        var saturation = Math.Clamp(S, 0, 255) / 255d;
        var lightness = Math.Clamp(L, 0, 255) / 255d;

        if (saturation <= 0d)
        {
            var value = ToByte(lightness * 255d);
            return new RgbColor(value, value, value);
        }

        var q = lightness < 0.5d
            ? lightness * (1d + saturation)
            : lightness + saturation - lightness * saturation;
        var p = 2d * lightness - q;

        return new RgbColor(
            ToByte(HueToRgb(p, q, hue + 1d / 3d) * 255d),
            ToByte(HueToRgb(p, q, hue) * 255d),
            ToByte(HueToRgb(p, q, hue - 1d / 3d) * 255d));
    }

    private static int NormalizeHue(int hue)
    {
        var normalized = hue % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0d)
        {
            t += 1d;
        }

        if (t > 1d)
        {
            t -= 1d;
        }

        if (t < 1d / 6d)
        {
            return p + (q - p) * 6d * t;
        }

        if (t < 1d / 2d)
        {
            return q;
        }

        if (t < 2d / 3d)
        {
            return p + (q - p) * (2d / 3d - t) * 6d;
        }

        return p;
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
}
