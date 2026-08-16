using System;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FAItemsRepeater.ElementIndexChanged" /> event
/// </summary>
public class FAItemsRepeaterElementIndexChangedEventArgs : EventArgs
{
	/// <summary>
	/// Get the element for which the index changed.
	/// </summary>
	public Control Element { get; private set; }

	/// <summary>
	/// Gets the index of the element after the change.
	/// </summary>
	public int NewIndex { get; private set; }

	/// <summary>
	/// Gets the index of the element before the change.
	/// </summary>
	public int OldIndex { get; private set; }

	internal FAItemsRepeaterElementIndexChangedEventArgs(Control element, int oldIndex, int newIndex)
	{
		Element = element;
		OldIndex = oldIndex;
		NewIndex = newIndex;
	}

	internal void Update(Control element, int oldIndex, int newIndex)
	{
		Element = element;
		NewIndex = newIndex;
		OldIndex = oldIndex;
	}
}
