using System;
using FluentAvalonia.UI.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FATabView.TabDragStarting" /> event
/// </summary>
public class FATabViewTabDragStartingEventArgs : EventArgs
{
	private DragItemsStartingEventArgs _innerArgs;

	/// <summary>
	/// Gets or sets a value that indicates whether the drag action should be cancelled
	/// </summary>
	public bool Cancel
	{
		get
		{
			return _innerArgs.Cancel;
		}
		set
		{
			_innerArgs.Cancel = value;
		}
	}

	/// <summary>
	/// Gets the data payload associated with a drag action
	/// </summary>
	public DataPackage Data => _innerArgs.Data;

	/// <summary>
	/// Gets the item taht was selected for the drag action
	/// </summary>
	public object Item { get; }

	/// <summary>
	/// Gets the TabViewItem that was selected for the drag action
	/// </summary>
	public FATabViewItem Tab { get; }

	internal FATabViewTabDragStartingEventArgs(DragItemsStartingEventArgs args, object item, FATabViewItem tab)
	{
		_innerArgs = args;
		Item = item;
		Tab = tab;
	}
}
