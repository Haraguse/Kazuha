using System;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides event data for the NumberBox.ValueChanged event.
/// </summary>
public class FANumberBoxValueChangedEventArgs : EventArgs
{
	/// <summary>
	/// Contains the old Value being replaced in a NumberBox.
	/// </summary>
	public double OldValue { get; }

	/// <summary>
	/// Contains the new Value to be set for a NumberBox.
	/// </summary>
	public double NewValue { get; }

	internal FANumberBoxValueChangedEventArgs(double oldV, double newV)
	{
		OldValue = oldV;
		NewValue = newV;
	}
}
