using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

/// <summary>
/// Common abstraction for a geometry to render.
/// It is implemented by both the immutable platform <see cref="T:Avalonia.Platform.IGeometryImpl" /> (which returns itself)
/// and the compositor-aware <see cref="T:Avalonia.Rendering.Composition.Server.ServerCompositionSimpleGeometry" /> server resource
/// (which returns its current, mutable backing geometry). This lets the render data stay strongly typed
/// while still resolving to the concrete <see cref="T:Avalonia.Platform.IGeometryImpl" /> that platform drawing contexts require.
/// </summary>
[PrivateApi]
public interface IRenderDataGeometry
{
	/// <summary>
	/// The underlying immutable platform geometry, or <c>null</c> when there's nothing to draw.
	/// </summary>
	IGeometryImpl? GeometryImpl { get; }
}
