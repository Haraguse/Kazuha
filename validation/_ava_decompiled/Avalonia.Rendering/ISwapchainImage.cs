using System;
using System.Threading.Tasks;

namespace Avalonia.Rendering;

internal interface ISwapchainImage : IAsyncDisposable
{
	PixelSize Size { get; }

	Task? LastPresent { get; }

	void BeginDraw();

	void Present();
}
