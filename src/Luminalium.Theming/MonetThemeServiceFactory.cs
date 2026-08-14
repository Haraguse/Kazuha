using System.Reflection;
using Luminalium.Core.Platform;

namespace Luminalium.Theming;

public static class MonetThemeServiceFactory
{
    private const string WindowsProviderTypeName =
        "Luminalium.Platform.Windows.Theming.WindowsDesktopColorProviders, Luminalium.Platform.Windows";

    public static MonetThemeService CreateDefault()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return new MonetThemeService();
        }

        var provider = TryCreateWindowsProvider();
        return new MonetThemeService(provider, provider);
    }

    private static WindowsDesktopColorProviderBridge? TryCreateWindowsProvider()
    {
        try
        {
            var providerType = Type.GetType(WindowsProviderTypeName, throwOnError: false);
            if (providerType is null || Activator.CreateInstance(providerType) is not { } provider)
            {
                return null;
            }

            return provider is IAccentProvider accentProvider && provider is IWallpaperProvider wallpaperProvider
                ? new WindowsDesktopColorProviderBridge(accentProvider, wallpaperProvider)
                : null;
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or TypeLoadException or MissingMethodException or MemberAccessException or TargetInvocationException)
        {
            return null;
        }
    }

    private sealed class WindowsDesktopColorProviderBridge(
        IAccentProvider accentProvider,
        IWallpaperProvider wallpaperProvider) : IAccentProvider, IWallpaperProvider
    {
        public Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default) =>
            accentProvider.TryGetAccentAsync(cancellationToken);

        public Task<PlatformOperationResult<string?>> TryGetWallpaperPathAsync(CancellationToken cancellationToken = default) =>
            wallpaperProvider.TryGetWallpaperPathAsync(cancellationToken);
    }
}
