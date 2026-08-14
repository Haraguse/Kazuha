using Luminalium.Core.Platform;

namespace Luminalium.Theming;

public static class MonetColorMath
{
    public const double MinimumPrimaryBackgroundContrast = 4.5d;

    public static RgbColor? ExtractSeedFromSamples(IReadOnlyList<RgbColor> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            return null;
        }

        var bucketCounts = new Dictionary<int, int>();
        foreach (var sample in samples)
        {
            var hsl = sample.ToHsl();
            if (hsl.S < 20 || hsl.L < 30 || hsl.L > 220)
            {
                continue;
            }

            var bucket = (hsl.H / 10) * 10;
            bucketCounts[bucket] = bucketCounts.TryGetValue(bucket, out var count)
                ? count + 1
                : 1;
        }

        if (bucketCounts.Count == 0)
        {
            return Average(samples);
        }

        var bestBucket = bucketCounts
            .OrderByDescending(bucket => bucket.Value)
            .ThenBy(bucket => bucket.Key)
            .First()
            .Key;
        var bucketSamples = samples
            .Where(sample =>
            {
                var hsl = sample.ToHsl();
                return hsl.S > 0 && (hsl.H / 10) * 10 == bestBucket;
            })
            .ToArray();

        return bucketSamples.Length == 0
            ? Average(samples)
            : Average(bucketSamples);
    }

    public static MonetPalette BuildPalette(RgbColor seed)
    {
        var hsl = seed.ToHsl();
        var saturation = hsl.S > 10
            ? Math.Max(hsl.S, 60)
            : hsl.S;

        var lightAccentLightness = Math.Max(80, Math.Min(130, hsl.L > 128 ? hsl.L - 40 : hsl.L));
        var darkAccentLightness = Math.Max(160, Math.Min(200, hsl.L + 50));

        var palette = new MonetPalette(
            seed,
            new ThemeVariantPalette(
                new HslColor(hsl.H, saturation, lightAccentLightness).ToRgb(),
                new HslColor(hsl.H, Math.Min(saturation, 15), 242).ToRgb(),
                RgbColor.FromHex("#FFFFFF"),
                RgbColor.FromHex("#000000")),
            new ThemeVariantPalette(
                new HslColor(hsl.H, saturation, darkAccentLightness).ToRgb(),
                new HslColor(hsl.H, Math.Min(saturation, 20), 20).ToRgb(),
                new HslColor(hsl.H, Math.Min(saturation, 15), 30).ToRgb(),
                RgbColor.FromHex("#FFFFFF")));

        return EnsureContrast(palette);
    }

    public static MonetPalette EnsureContrast(MonetPalette palette) =>
        palette with
        {
            Light = EnsureVariantContrast(palette.Light, darkenPrimary: true),
            Dark = EnsureVariantContrast(palette.Dark, darkenPrimary: false),
        };

    public static MonetPalette HighContrastPalette() =>
        new(
            RgbColor.FromHex("#FFFF00"),
            new ThemeVariantPalette(
                RgbColor.FromHex("#000000"),
                RgbColor.FromHex("#FFFFFF"),
                RgbColor.FromHex("#FFFFFF"),
                RgbColor.FromHex("#000000")),
            new ThemeVariantPalette(
                RgbColor.FromHex("#FFFF00"),
                RgbColor.FromHex("#000000"),
                RgbColor.FromHex("#000000"),
                RgbColor.FromHex("#FFFFFF")));

    public static MonetPalette FallbackPalette() =>
        BuildPalette(RgbColor.FromHex("#0078D4"));

    public static double ContrastRatio(RgbColor first, RgbColor second)
    {
        var firstLuminance = RelativeLuminance(first);
        var secondLuminance = RelativeLuminance(second);
        var lighter = Math.Max(firstLuminance, secondLuminance);
        var darker = Math.Min(firstLuminance, secondLuminance);
        return (lighter + 0.05d) / (darker + 0.05d);
    }

    public static PlatformOperationWarning ToWarning(PlatformOperationError error) =>
        new(error.Code, error.Message, error.Detail);

    private static ThemeVariantPalette EnsureVariantContrast(ThemeVariantPalette variant, bool darkenPrimary)
    {
        if (ContrastRatio(variant.Primary, variant.Background) >= MinimumPrimaryBackgroundContrast)
        {
            return variant;
        }

        var hsl = variant.Primary.ToHsl();
        var primary = variant.Primary;
        var targetLightness = darkenPrimary ? 0 : 255;
        var step = darkenPrimary ? -1 : 1;
        for (var lightness = hsl.L; lightness != targetLightness + step; lightness += step)
        {
            primary = new HslColor(hsl.H, hsl.S, lightness).ToRgb();
            if (ContrastRatio(primary, variant.Background) >= MinimumPrimaryBackgroundContrast)
            {
                return variant with { Primary = primary };
            }
        }

        return variant with { Primary = primary };
    }

    private static RgbColor Average(IReadOnlyList<RgbColor> samples)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        foreach (var sample in samples)
        {
            red += sample.R;
            green += sample.G;
            blue += sample.B;
        }

        return new RgbColor(
            (byte)(red / samples.Count),
            (byte)(green / samples.Count),
            (byte)(blue / samples.Count));
    }

    private static double RelativeLuminance(RgbColor color) =>
        0.2126d * Linearize(color.R) + 0.7152d * Linearize(color.G) + 0.0722d * Linearize(color.B);

    private static double Linearize(byte channel)
    {
        var value = channel / 255d;
        return value <= 0.03928d
            ? value / 12.92d
            : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
    }
}
