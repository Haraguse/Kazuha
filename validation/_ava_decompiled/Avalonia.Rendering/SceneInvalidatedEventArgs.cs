using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering;

/// <summary>
/// Provides data for the <see cref="E:Avalonia.Rendering.IRenderer.SceneInvalidated" /> event.
/// </summary>
[PrivateApi]
public class SceneInvalidatedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the invalidated area.
	/// </summary>
	public Rect DirtyRect { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rendering.SceneInvalidatedEventArgs" /> class.
	/// </summary>
	/// <param name="dirtyRect">The updated area.</param>
	public SceneInvalidatedEventArgs(Rect dirtyRect)
	{
		DirtyRect = dirtyRect;
	}
}
