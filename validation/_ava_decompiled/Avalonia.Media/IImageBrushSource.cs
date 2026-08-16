using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media;

[NotClientImplementable]
public interface IImageBrushSource
{
	internal IRef<IBitmapImpl>? Bitmap { get; }
}
