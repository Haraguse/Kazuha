using System;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FABreadcrumbBar.ItemClicked" /> event
/// </summary>
public class FABreadcrumbBarItemClickedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the index of the item that was clicked.
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Gets the Content property value of the BreadcrumbBarItem that is clicked.
	/// </summary>
	public object Item { get; }

	internal FABreadcrumbBarItemClickedEventArgs(int index, object item)
	{
		Index = index;
		Item = item;
	}
}
