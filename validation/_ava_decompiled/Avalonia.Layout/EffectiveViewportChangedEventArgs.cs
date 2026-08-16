using System;

namespace Avalonia.Layout;

/// <summary>
/// Provides data for the <see cref="E:Avalonia.Layout.Layoutable.EffectiveViewportChanged" /> event.
/// </summary>
public class EffectiveViewportChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the <see cref="T:Avalonia.Rect" /> representing the effective viewport.
	/// </summary>
	/// <remarks>
	/// The viewport is expressed in coordinates relative to the control that the event is
	/// raised on.
	/// </remarks>
	public Rect EffectiveViewport { get; }

	public EffectiveViewportChangedEventArgs(Rect effectiveViewport)
	{
		EffectiveViewport = effectiveViewport;
	}
}
