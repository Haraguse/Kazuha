using Luminalium.Core.Platform;

namespace Luminalium.Theming;

public interface IWallpaperProvider
{
    Task<PlatformOperationResult<string?>> TryGetWallpaperPathAsync(CancellationToken cancellationToken = default);
}

public interface IAccentProvider
{
    Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default);
}
