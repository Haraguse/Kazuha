using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal interface IDirtyRectCollector
{
	void AddRect(LtrbRect rect);
}
