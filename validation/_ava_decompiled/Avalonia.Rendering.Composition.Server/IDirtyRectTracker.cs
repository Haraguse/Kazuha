using System;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal interface IDirtyRectTracker : IDirtyRectCollector
{
	bool IsEmpty { get; }

	LtrbRect CombinedRect { get; }

	/// <summary>
	/// Post-processes the dirty rect area (e. g. to account for anti-aliasing)
	/// </summary>
	void FinalizeFrame(LtrbRect bounds);

	IDisposable BeginDraw(IDrawingContextImpl ctx);

	bool Intersects(LtrbRect rect);

	void Initialize(LtrbRect bounds);

	void Visualize(IDrawingContextImpl context);
}
