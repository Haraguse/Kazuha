using Luminalium.Core.Platform;
using SkiaSharp;

namespace Luminalium.Theming;

public sealed class MonetImageAnalyzer
{
    private const int MaxDimension = 100;
    private const long MaxEncodedBytes = 25L * 1024L * 1024L;
    private const int MaxSourceDimension = 20_000;
    private const long MaxSourcePixels = 100_000_000L;

    public static async Task<PlatformOperationResult<IReadOnlyList<RgbColor>>> ExtractSamplesAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Failure(PlatformOperationErrorCode.NotFound, "Wallpaper image path is missing.");
        }

        try
        {
            var pathValidation = ValidateLocalImagePath(imagePath);
            if (!pathValidation.IsSuccess)
            {
                return PlatformOperation.Failure<IReadOnlyList<RgbColor>>(pathValidation.Error!);
            }

            await using var stream = File.OpenRead(imagePath);
            return ExtractSamples(stream);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(PlatformOperationErrorCode.AccessDenied, "Wallpaper image could not be read.", exception.Message);
        }
        catch (IOException exception)
        {
            return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image could not be read.", exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image analysis was cancelled.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image could not be read.", exception.Message);
        }
    }

    public static PlatformOperationResult<IReadOnlyList<RgbColor>> ExtractSamples(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image format is unsupported or malformed.");
        }

        using var stream = new MemoryStream(imageBytes, writable: false);
        return ExtractSamples(stream);
    }

    private static PlatformOperationResult<IReadOnlyList<RgbColor>> ExtractSamples(Stream stream)
    {
        try
        {
            using var codec = SKCodec.Create(stream);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0)
            {
                return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image format is unsupported or malformed.");
            }

            var sourceInfo = codec.Info;
            if (sourceInfo.Width > MaxSourceDimension || sourceInfo.Height > MaxSourceDimension)
            {
                return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image dimensions exceed the Monet analysis limit.");
            }

            if ((long)sourceInfo.Width * sourceInfo.Height > MaxSourcePixels)
            {
                return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image pixel count exceeds the Monet analysis limit.");
            }

            using var scaled = DecodeAnalysisBitmap(codec, sourceInfo);
            if (scaled is null)
            {
                return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image format is unsupported or malformed.");
            }

            var samples = new List<RgbColor>();
            for (var y = 0; y < scaled.Height; y += 2)
            {
                for (var x = 0; x < scaled.Width; x += 2)
                {
                    var pixel = scaled.GetPixel(x, y);
                    if (pixel.Alpha < 128)
                    {
                        continue;
                    }

                    samples.Add(new RgbColor(pixel.Red, pixel.Green, pixel.Blue));
                }
            }

            return samples.Count == 0
                ? Failure(PlatformOperationErrorCode.NotFound, "Wallpaper image did not contain opaque pixels.")
                : PlatformOperation.Success<IReadOnlyList<RgbColor>>(samples);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Failure(PlatformOperationErrorCode.Failed, "Wallpaper image format is unsupported or malformed.", exception.Message);
        }
    }

    private static SKBitmap? DecodeAnalysisBitmap(SKCodec codec, SKImageInfo sourceInfo)
    {
        var scale = Math.Min(1d, MaxDimension / (double)Math.Max(sourceInfo.Width, sourceInfo.Height));
        var scaledDimensions = codec.GetScaledDimensions((float)scale);
        var width = Math.Clamp(scaledDimensions.Width, 1, MaxDimension);
        var height = Math.Clamp(scaledDimensions.Height, 1, MaxDimension);
        var targetInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(targetInfo);
        var result = codec.GetPixels(targetInfo, bitmap.GetPixels(), bitmap.RowBytes, new SKCodecOptions(0));
        if (result is SKCodecResult.Success or SKCodecResult.IncompleteInput)
        {
            return bitmap;
        }

        bitmap.Dispose();
        return null;
    }

    private static PlatformOperationResult ValidateLocalImagePath(string imagePath)
    {
        if (imagePath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Unavailable,
                "Network wallpaper paths are not used for Monet analysis."));
        }

        var attributes = File.GetAttributes(imagePath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Unavailable,
                "Reparse-point wallpaper paths are not used for Monet analysis."));
        }

        var length = new FileInfo(imagePath).Length;
        return length > MaxEncodedBytes
            ? PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "Wallpaper image file exceeds the Monet analysis size limit."))
            : PlatformOperationResult.Success();
    }

    private static PlatformOperationResult<IReadOnlyList<RgbColor>> Failure(
        PlatformOperationErrorCode code,
        string message,
        string? detail = null) =>
        PlatformOperation.Failure<IReadOnlyList<RgbColor>>(new PlatformOperationError(code, message, detail));
}
