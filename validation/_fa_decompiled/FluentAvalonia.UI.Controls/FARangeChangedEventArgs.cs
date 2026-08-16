using System;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Event data for the <see cref="E:FluentAvalonia.UI.Controls.FARangeSlider.ValueChanged" /> event
/// </summary>
public class FARangeChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the old value for the property identified by <see cref="P:FluentAvalonia.UI.Controls.FARangeChangedEventArgs.ChangedProperty" />
	/// </summary>
	public double OldValue { get; }

	/// <summary>
	/// Gets the new value for the property identified by <see cref="P:FluentAvalonia.UI.Controls.FARangeChangedEventArgs.ChangedProperty" />
	/// </summary>
	public double NewValue { get; }

	/// <summary>
	/// Gets the property that changed to trigger this event
	/// </summary>
	public FARangeSelectorProperty ChangedProperty { get; }

	internal FARangeChangedEventArgs(double oldValue, double newValue, FARangeSelectorProperty prop)
	{
		OldValue = oldValue;
		NewValue = newValue;
		ChangedProperty = prop;
	}
}
