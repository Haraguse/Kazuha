using System;

namespace Avalonia.Platform;

public interface IDrawingContextLayerImpl : IBitmapImpl, IDisposable
{
	/// <summary>
	/// Returns true if layer supports optimized blit.
	/// </summary>
	bool CanBlit { get; }

	/// <summary>
	/// Indicates if the render target is no longer usable and needs to be recreated
	/// </summary>
	bool IsCorrupted { get; }

	/// <summary>
	/// Does optimized blit with Src blend mode.
	/// </summary>
	/// <param name="context"></param>
	void Blit(IDrawingContextImpl context);

	/// <summary>
	/// Creates drawing context. It matches the properties of the original drawing context this layer was created from.
	/// </summary>
	IDrawingContextImpl CreateDrawingContext();
}
