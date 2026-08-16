using Avalonia.Metadata;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Platform;

/// <summary>
/// Represents a geometry with a transform applied.
/// </summary>
/// <remarks>
/// An <see cref="T:Avalonia.Platform.ITransformedGeometryImpl" /> transforms a geometry without transforming its
/// stroke thickness.
/// </remarks>
[Unstable]
public interface ITransformedGeometryImpl : IGeometryImpl, IRenderDataGeometry
{
	/// <summary>
	/// Gets the source geometry that the <see cref="P:Avalonia.Platform.ITransformedGeometryImpl.Transform" /> is applied to.
	/// </summary>
	IGeometryImpl SourceGeometry { get; }

	/// <summary>
	/// Gets the applied transform.
	/// </summary>
	Matrix Transform { get; }
}
