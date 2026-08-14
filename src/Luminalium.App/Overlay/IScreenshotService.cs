using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Luminalium.App.Overlay;

public interface IScreenshotService
{
    Task<string> CaptureAsync(Control target, string filePath, CancellationToken cancellationToken = default);
}

public sealed class RenderTargetBitmapScreenshotService : IScreenshotService
{
    public Task<string> CaptureAsync(Control target, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var scaling = TopLevel.GetTopLevel(target)?.RenderScaling ?? 1.0;
        var width = Math.Max(1, (int)Math.Ceiling(target.Bounds.Width * scaling));
        var height = Math.Max(1, (int)Math.Ceiling(target.Bounds.Height * scaling));
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96 * scaling, 96 * scaling));
        bitmap.Render(target);
        using var stream = File.Create(filePath);
        bitmap.Save(stream, new PngBitmapEncoderOptions());
        return Task.FromResult(filePath);
    }
}
