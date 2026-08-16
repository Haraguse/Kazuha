using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform.Surfaces;

namespace Avalonia.Platform;

[Unstable]
[PrivateApi]
public interface IPlatformRenderInterfaceContext : IOptionalFeatureProvider, IDisposable
{
	/// <summary>
	/// Indicates that the context is no longer usable. This method should be thread-safe
	/// </summary>
	bool IsLost { get; }

	/// <summary>
	/// Exposes features that should be available for consumption while context isn't active (e. g. from the UI thread)
	/// </summary>
	IReadOnlyDictionary<Type, object> PublicFeatures { get; }

	/// <summary>
	/// Maximum supported offscreen render target pixel size, or null if no limit
	/// </summary>
	PixelSize? MaxOffscreenRenderTargetPixelSize { get; }

	/// <summary>
	/// Creates a renderer.
	/// </summary>
	/// <param name="surfaces">
	/// The list of native platform surfaces that can be used for output.
	/// </param>
	/// <returns>An <see cref="T:Avalonia.Platform.IRenderTarget" />.</returns>
	IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);

	/// <summary>
	/// Creates an offscreen render target 
	/// </summary>
	/// <param name="pixelSize">The size, in pixels, of the render target</param>
	/// <param name="scaling">The scaling which will be reported by IBitmap.Dpi</param>
	/// <param name="enableTextAntialiasing">Specifies if text antialiasing should be enabled</param>
	/// <returns></returns>
	IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling, bool enableTextAntialiasing);

	/// <summary>
	/// Checks if a render target can be created for the given surfaces and the preferred surface is ready
	/// </summary>
	bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
	{
		return true;
	}
}
