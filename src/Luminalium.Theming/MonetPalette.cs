using Luminalium.Core.Platform;

namespace Luminalium.Theming;

public sealed record ThemeVariantPalette(
    RgbColor Primary,
    RgbColor Background,
    RgbColor Surface,
    RgbColor Text);

public sealed record MonetPalette(
    RgbColor Seed,
    ThemeVariantPalette Light,
    ThemeVariantPalette Dark);

public sealed record MonetThemeResult(
    MonetPalette Palette,
    bool UsedFallback,
    IReadOnlyList<PlatformOperationWarning> Warnings);
