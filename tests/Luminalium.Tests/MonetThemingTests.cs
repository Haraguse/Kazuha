using Luminalium.Core.Platform;
using Luminalium.Theming;
using SkiaSharp;
using Xunit;

namespace Luminalium.Tests;

public sealed class MonetThemingTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-Monet-{Guid.NewGuid():N}");

    public MonetThemingTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void ExtractSeedUsesDominantHueBucketAverage()
    {
        var seed = MonetColorMath.ExtractSeedFromSamples(
        [
            new RgbColor(220, 20, 20),
            new RgbColor(200, 40, 40),
            new RgbColor(20, 60, 220),
        ]);

        Assert.Equal("#D21E1E", seed!.Value.ToHexString());
    }

    [Fact]
    public void ExtractSeedFallsBackToWholeAverageWhenSamplesAreDesaturated()
    {
        var seed = MonetColorMath.ExtractSeedFromSamples(
        [
            new RgbColor(100, 100, 100),
            new RgbColor(200, 200, 200),
        ]);

        Assert.Equal("#969696", seed!.Value.ToHexString());
        Assert.Null(MonetColorMath.ExtractSeedFromSamples([]));
    }

    [Fact]
    public void BuildPalettePinsFallbackSeedColors()
    {
        var palette = MonetColorMath.BuildPalette(RgbColor.FromHex("#0078D4"));

        Assert.Equal("#0078D4", palette.Seed.ToHexString());
        Assert.Equal("#0070C6", palette.Light.Primary.ToHexString());
        Assert.Equal("#F1F2F3", palette.Light.Background.ToHexString());
        Assert.Equal("#FFFFFF", palette.Light.Surface.ToHexString());
        Assert.Equal("#000000", palette.Light.Text.ToHexString());
        Assert.Equal("#41ADFF", palette.Dark.Primary.ToHexString());
        Assert.Equal("#121416", palette.Dark.Background.ToHexString());
        Assert.Equal("#1C1E20", palette.Dark.Surface.ToHexString());
        Assert.Equal("#FFFFFF", palette.Dark.Text.ToHexString());
    }

    [Fact]
    public void BuildPaletteAppliesSaturationBoostAndLightnessClamps()
    {
        var veryDark = MonetColorMath.BuildPalette(RgbColor.FromHex("#040810"));
        var veryLight = MonetColorMath.BuildPalette(RgbColor.FromHex("#F8FAFF"));

        Assert.Equal("#204080", veryDark.Light.Primary.ToHexString());
        Assert.Equal("#678DD9", veryDark.Dark.Primary.ToHexString());
        Assert.Equal("#054CFF", veryLight.Light.Primary.ToHexString());
        Assert.Equal("#91B0FF", veryLight.Dark.Primary.ToHexString());
    }

    [Fact]
    public void EnsureContrastAdjustsLowContrastPrimaryAndLeavesHighContrastPrimary()
    {
        var lowContrast = new MonetPalette(
            RgbColor.FromHex("#808080"),
            new ThemeVariantPalette(
                RgbColor.FromHex("#F0F0F0"),
                RgbColor.FromHex("#FFFFFF"),
                RgbColor.FromHex("#FFFFFF"),
                RgbColor.FromHex("#000000")),
            new ThemeVariantPalette(
                RgbColor.FromHex("#111111"),
                RgbColor.FromHex("#000000"),
                RgbColor.FromHex("#111111"),
                RgbColor.FromHex("#FFFFFF")));
        var highContrast = MonetColorMath.HighContrastPalette();

        var adjusted = MonetColorMath.EnsureContrast(lowContrast);
        var unchanged = MonetColorMath.EnsureContrast(highContrast);

        Assert.True(MonetColorMath.ContrastRatio(adjusted.Light.Primary, adjusted.Light.Background) >= 4.5d);
        Assert.True(MonetColorMath.ContrastRatio(adjusted.Dark.Primary, adjusted.Dark.Background) >= 4.5d);
        Assert.NotEqual("#F0F0F0", adjusted.Light.Primary.ToHexString());
        Assert.Equal(highContrast, unchanged);
    }

    [Fact]
    public void HighContrastAndFallbackPalettesAreDeterministic()
    {
        Assert.Equal(MonetColorMath.HighContrastPalette(), MonetColorMath.HighContrastPalette());
        Assert.Equal(MonetColorMath.FallbackPalette(), MonetColorMath.FallbackPalette());
    }

    [Fact]
    public async Task ThemeServiceUsesAccentBeforeWallpaper()
    {
        var wallpaper = WritePng("ignored-wallpaper.png", new RgbColor(255, 0, 0));
        var service = new MonetThemeService(
            new FakeAccentProvider(RgbColor.FromHex("#0078D4")),
            new FakeWallpaperProvider(wallpaper));

        var result = await service.BuildPaletteAsync();

        Assert.False(result.UsedFallback);
        Assert.Empty(result.Warnings);
        Assert.Equal("#0078D4", result.Palette.Seed.ToHexString());
    }

    [Fact]
    public async Task ThemeServiceUsesWallpaperWhenAccentIsMissing()
    {
        var wallpaper = WritePng("red-wallpaper.png", new RgbColor(255, 0, 0));
        var service = new MonetThemeService(
            new FakeAccentProvider(null),
            new FakeWallpaperProvider(wallpaper));

        var result = await service.BuildPaletteAsync();

        Assert.False(result.UsedFallback);
        Assert.Contains(result.Warnings, warning => warning.Code == PlatformOperationErrorCode.NotFound);
        Assert.Equal("#FF0000", result.Palette.Seed.ToHexString());
    }

    [Fact]
    public async Task ThemeServiceFallsBackWithWarningWhenProvidersAreMissing()
    {
        var service = new MonetThemeService(
            new FakeAccentProvider(null),
            new FakeWallpaperProvider(null));

        var result = await service.BuildPaletteAsync();

        Assert.True(result.UsedFallback);
        Assert.Equal(MonetColorMath.FallbackPalette(), result.Palette);
        Assert.Contains(result.Warnings, warning => warning.Message.Contains("fallback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ThemeServiceFallsBackWhenWallpaperImageIsMalformed()
    {
        var malformed = Path.Combine(_directory, "malformed.png");
        File.WriteAllText(malformed, "not an image");
        var service = new MonetThemeService(
            new FakeAccentProvider(null),
            new FakeWallpaperProvider(malformed));

        var result = await service.BuildPaletteAsync();

        Assert.True(result.UsedFallback);
        Assert.Equal(MonetColorMath.FallbackPalette(), result.Palette);
        Assert.Contains(result.Warnings, warning => warning.Message.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ThemeServiceFallsBackWhenWallpaperPathIsUnsafeOrOversized()
    {
        var oversized = Path.Combine(_directory, "oversized.bin");
        using (var stream = File.Create(oversized))
        {
            stream.SetLength(26L * 1024L * 1024L);
        }

        var oversizedService = new MonetThemeService(
            new FakeAccentProvider(null),
            new FakeWallpaperProvider(oversized));
        var networkPathService = new MonetThemeService(
            new FakeAccentProvider(null),
            new FakeWallpaperProvider(@"\\server\share\wallpaper.png"));

        var oversizedResult = await oversizedService.BuildPaletteAsync();
        var networkPathResult = await networkPathService.BuildPaletteAsync();

        Assert.True(oversizedResult.UsedFallback);
        Assert.Contains(oversizedResult.Warnings, warning => warning.Message.Contains("size limit", StringComparison.OrdinalIgnoreCase));
        Assert.True(networkPathResult.UsedFallback);
        Assert.Contains(networkPathResult.Warnings, warning => warning.Message.Contains("Network wallpaper", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ThemeServiceReturnsFixedHighContrastPaletteWhenRequested()
    {
        var service = new MonetThemeService(
            new FakeAccentProvider(RgbColor.FromHex("#0078D4")),
            new FakeWallpaperProvider(null));

        var result = await service.BuildPaletteAsync(highContrast: true);

        Assert.False(result.UsedFallback);
        Assert.Empty(result.Warnings);
        Assert.Equal(MonetColorMath.HighContrastPalette(), result.Palette);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string WritePng(string fileName, RgbColor color)
    {
        var path = Path.Combine(_directory, fileName);
        using var bitmap = new SKBitmap(12, 12);
        bitmap.Erase(new SKColor(color.R, color.G, color.B, 255));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private sealed class FakeAccentProvider(RgbColor? color) : IAccentProvider
    {
        public Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(color is { } value
                ? PlatformOperation.Success<RgbColor?>(value)
                : PlatformOperation.Failure<RgbColor?>(new PlatformOperationError(
                    PlatformOperationErrorCode.NotFound,
                    "Accent missing.")));
    }

    private sealed class FakeWallpaperProvider(string? path) : IWallpaperProvider
    {
        public Task<PlatformOperationResult<string?>> TryGetWallpaperPathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(path is null
                ? PlatformOperation.Failure<string?>(new PlatformOperationError(
                    PlatformOperationErrorCode.NotFound,
                    "Wallpaper missing."))
                : PlatformOperation.Success<string?>(path));
    }
}
