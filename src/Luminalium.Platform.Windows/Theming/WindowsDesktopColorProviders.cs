using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Luminalium.Core.Platform;
using Luminalium.Theming;
using Microsoft.Win32;

namespace Luminalium.Platform.Windows.Theming;

public sealed class WindowsDesktopColorProviders : IWallpaperProvider, IAccentProvider
{
    private const int SpiGetDeskWallpaper = 0x0073;
    private const int MaxWallpaperPath = 260;
    private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";
    private const string ExplorerAccentKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

    public Task<PlatformOperationResult<string?>> TryGetWallpaperPathAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(PlatformOperation.Failure<string?>(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "Wallpaper lookup was cancelled.")));
        }

        try
        {
            var buffer = new char[MaxWallpaperPath];
            if (!SystemParametersInfo(SpiGetDeskWallpaper, MaxWallpaperPath, buffer, 0))
            {
                return Task.FromResult(PlatformOperation.Failure<string?>(LastError("SPI_GETDESKWALLPAPER failed.")));
            }

            var terminator = Array.IndexOf(buffer, '\0');
            var path = new string(buffer, 0, terminator >= 0 ? terminator : buffer.Length);
            return Task.FromResult(File.Exists(path)
                ? PlatformOperation.Success<string?>(path)
                : PlatformOperation.Failure<string?>(new PlatformOperationError(
                    PlatformOperationErrorCode.NotFound,
                    "Windows desktop wallpaper path is missing or does not exist.",
                    string.IsNullOrWhiteSpace(path) ? null : path)));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException or IOException or Win32Exception or PlatformNotSupportedException or DllNotFoundException or EntryPointNotFoundException)
        {
            return Task.FromResult(PlatformOperation.Failure<string?>(ToError(exception)));
        }
    }

    public Task<PlatformOperationResult<RgbColor?>> TryGetAccentAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(PlatformOperation.Failure<RgbColor?>(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "Accent color lookup was cancelled.")));
        }

        try
        {
            var colorizationColor = ReadDword(DwmKeyPath, "ColorizationColor");
            if (TryFromArgbDword(colorizationColor, requireAlpha: true, out var dwmAccent))
            {
                return Task.FromResult(PlatformOperation.Success<RgbColor?>(dwmAccent));
            }

            var accentColorMenu = ReadDword(ExplorerAccentKeyPath, "AccentColorMenu");
            if (TryFromArgbDword(accentColorMenu, requireAlpha: false, out var menuAccent))
            {
                return Task.FromResult(PlatformOperation.Success<RgbColor?>(menuAccent));
            }

            return Task.FromResult(PlatformOperation.Failure<RgbColor?>(new PlatformOperationError(
                PlatformOperationErrorCode.NotFound,
                "Windows accent color registry values were not found.")));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException or IOException or Win32Exception or PlatformNotSupportedException or DllNotFoundException or EntryPointNotFoundException)
        {
            return Task.FromResult(PlatformOperation.Failure<RgbColor?>(ToError(exception)));
        }
    }

    private static uint? ReadDword(string path, string valueName)
    {
        using var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            .OpenSubKey(path, writable: false);
        var value = key?.GetValue(valueName);
        return value switch
        {
            int signed => unchecked((uint)signed),
            uint unsigned => unsigned,
            _ => null,
        };
    }

    private static bool TryFromArgbDword(uint? value, bool requireAlpha, out RgbColor color)
    {
        color = default;
        if (value is null)
        {
            return false;
        }

        var raw = value.Value;
        var alpha = (byte)((raw >> 24) & 0xFF);
        if (requireAlpha && alpha == 0)
        {
            return false;
        }

        color = new RgbColor(
            (byte)((raw >> 16) & 0xFF),
            (byte)((raw >> 8) & 0xFF),
            (byte)(raw & 0xFF));
        return true;
    }

    private static PlatformOperationError LastError(string message)
    {
        var lastError = Marshal.GetLastPInvokeError();
        return new PlatformOperationError(
            PlatformOperationErrorCode.Failed,
            message,
            lastError == 0 ? null : new Win32Exception(lastError).Message,
            lastError == 0 ? null : lastError);
    }

    private static PlatformOperationError ToError(Exception exception)
    {
        var code = exception is UnauthorizedAccessException or SecurityException
            ? PlatformOperationErrorCode.AccessDenied
            : PlatformOperationErrorCode.Failed;
        return new PlatformOperationError(code, exception.Message, exception.GetType().Name);
    }

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        int action,
        int parameter,
        [Out] char[] value,
        int flags);
}
