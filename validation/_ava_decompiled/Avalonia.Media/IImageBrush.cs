using Avalonia.Metadata;

namespace Avalonia.Media;

/// <summary>
/// Paints an area with an <see cref="T:Avalonia.Media.Imaging.IBitmap" />.
/// </summary>
[NotClientImplementable]
public interface IImageBrush : ITileBrush, IBrush
{
	/// <summary>
	/// Gets the image to draw.
	/// </summary>
	IImageBrushSource? Source { get; }
}
