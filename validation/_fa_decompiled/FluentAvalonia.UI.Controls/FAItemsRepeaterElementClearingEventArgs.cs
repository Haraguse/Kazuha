using System;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FAItemsRepeater.ElementClearing" /> event
/// </summary>
public class FAItemsRepeaterElementClearingEventArgs : EventArgs
{
	/// <summary>
	/// Gets the element that is being cleared for re-use.
	/// </summary>
	public Control Element { get; private set; }

	internal FAItemsRepeaterElementClearingEventArgs(Control element)
	{
		Element = element;
	}

	internal void Update(Control element)
	{
		Element = element;
	}
}
