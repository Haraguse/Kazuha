using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Defines the platform-specific interface for a <see cref="T:Avalonia.Media.Imaging.Bitmap" />.
/// </summary>
[PrivateApi]
public interface IBitmapImpl : IDisposable
{
	/// <summary>
	/// Gets the dots per inch (DPI) of the image.
	/// </summary>
	Vector Dpi { get; }

	/// <summary>
	/// Gets the size of the bitmap, in device pixels.
	/// </summary>
	PixelSize PixelSize { get; }

	/// <summary>
	/// Version of the pixel data
	/// </summary>
	int Version { get; }

	/// <summary>
	/// Saves the bitmap to a stream with the specified options.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="options">
	/// The options specifying the format and settings to use.
	/// Typical usages include <see cref="T:Avalonia.Media.Imaging.PngBitmapEncoderOptions" /> and <see cref="T:Avalonia.Media.Imaging.JpegBitmapEncoderOptions" />.
	/// </param>
	void Save(Stream stream, BitmapEncoderOptions options);
}
