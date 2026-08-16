using System;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FATabView.TabDroppedOutside" /> event
/// </summary>
public class FATabViewTabDroppedOutsideEventArgs : EventArgs
{
	/// <summary>
	/// Gets the item that was dropped outside of the TabStrip
	/// </summary>
	public object Item { get; }

	/// <summary>
	/// Gets the TabViewItem that was dropped outside of the TabStrip
	/// </summary>
	public FATabViewItem Tab { get; }

	internal FATabViewTabDroppedOutsideEventArgs(object item, FATabViewItem tab)
	{
		Item = item;
		Tab = tab;
	}
}
