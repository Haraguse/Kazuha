using Avalonia.Metadata;

namespace Avalonia.Media;

[NotClientImplementable]
public interface ISceneBrush : ITileBrush, IBrush
{
	ISceneBrushContent? CreateContent();
}
