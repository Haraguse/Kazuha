using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[Unstable]
[PrivateApi]
public interface IPlatformRenderInterfaceRegion : IDisposable
{
	bool IsEmpty { get; }

	LtrbPixelRect Bounds { get; }

	IList<LtrbPixelRect> Rects { get; }

	void AddRect(LtrbPixelRect rect);

	void Reset();

	bool Intersects(LtrbRect rect);

	bool Contains(Point pt);
}
