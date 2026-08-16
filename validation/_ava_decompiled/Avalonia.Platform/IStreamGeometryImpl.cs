using Avalonia.Metadata;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Platform;

/// <summary>
/// Defines the platform-specific interface for a <see cref="T:Avalonia.Media.StreamGeometry" />.
/// </summary>
[Unstable]
public interface IStreamGeometryImpl : IGeometryImpl, IRenderDataGeometry
{
	/// <summary>
	/// Clones the geometry.
	/// </summary>
	/// <returns>A cloned geometry.</returns>
	IStreamGeometryImpl Clone();

	/// <summary>
	/// Opens the geometry to start defining it.
	/// </summary>
	/// <returns>
	/// An <see cref="T:Avalonia.Platform.IStreamGeometryContextImpl" /> which can be used to define the geometry.
	/// </returns>
	IStreamGeometryContextImpl Open();
}
