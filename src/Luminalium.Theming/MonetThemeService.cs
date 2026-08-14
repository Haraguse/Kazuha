using Luminalium.Core.Platform;

namespace Luminalium.Theming;

public sealed class MonetThemeService(
    IAccentProvider? accentProvider = null,
    IWallpaperProvider? wallpaperProvider = null)
{
    public async Task<MonetThemeResult> BuildPaletteAsync(
        bool highContrast = false,
        CancellationToken cancellationToken = default)
    {
        if (highContrast)
        {
            return new MonetThemeResult(MonetColorMath.HighContrastPalette(), UsedFallback: false, []);
        }

        var warnings = new List<PlatformOperationWarning>();
        if (accentProvider is not null)
        {
            var accent = await TryProviderAsync(
                () => accentProvider.TryGetAccentAsync(cancellationToken),
                "Accent provider failed.").ConfigureAwait(false);
            if (accent.IsSuccess && accent.Value is { } accentSeed)
            {
                return new MonetThemeResult(MonetColorMath.BuildPalette(accentSeed), UsedFallback: false, warnings);
            }

            AddWarning(warnings, accent.Error, "Windows accent provider returned no color.");
        }

        if (wallpaperProvider is not null)
        {
            var wallpaper = await TryProviderAsync(
                () => wallpaperProvider.TryGetWallpaperPathAsync(cancellationToken),
                "Wallpaper provider failed.").ConfigureAwait(false);
            if (wallpaper.IsSuccess && !string.IsNullOrWhiteSpace(wallpaper.Value))
            {
                var samples = await MonetImageAnalyzer.ExtractSamplesAsync(wallpaper.Value!, cancellationToken).ConfigureAwait(false);
                if (samples.IsSuccess)
                {
                    var seed = MonetColorMath.ExtractSeedFromSamples(samples.Value);
                    if (seed is { } wallpaperSeed)
                    {
                        return new MonetThemeResult(MonetColorMath.BuildPalette(wallpaperSeed), UsedFallback: false, warnings);
                    }

                    warnings.Add(new PlatformOperationWarning(
                        PlatformOperationErrorCode.NotFound,
                        "Wallpaper image did not produce a Monet seed."));
                }
                else
                {
                    AddWarning(warnings, samples.Error, "Wallpaper image analysis failed.");
                }
            }
            else
            {
                AddWarning(warnings, wallpaper.Error, "Wallpaper provider returned no image path.");
            }
        }

        warnings.Add(new PlatformOperationWarning(
            PlatformOperationErrorCode.NotFound,
            "Using deterministic fallback Monet palette.",
            "No system accent color or usable wallpaper seed was available."));
        return new MonetThemeResult(MonetColorMath.FallbackPalette(), UsedFallback: true, warnings);
    }

    private static async Task<PlatformOperationResult<T>> TryProviderAsync<T>(
        Func<Task<PlatformOperationResult<T>>> operation,
        string message)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PlatformOperation.Failure<T>(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                message,
                exception.Message));
        }
    }

    private static void AddWarning(
        List<PlatformOperationWarning> warnings,
        PlatformOperationError? error,
        string fallbackMessage)
    {
        if (error is not null)
        {
            warnings.Add(MonetColorMath.ToWarning(error));
            return;
        }

        warnings.Add(new PlatformOperationWarning(PlatformOperationErrorCode.NotFound, fallbackMessage));
    }
}
