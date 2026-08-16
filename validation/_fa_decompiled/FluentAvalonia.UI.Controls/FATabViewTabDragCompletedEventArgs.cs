using System;
using Avalonia.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FATabView.TabDragCompleted" /> event
/// </summary>
public class FATabViewTabDragCompletedEventArgs : EventArgs
{
	private DragItemsCompletedEventArgs _innerArgs;

	/// <summary>
	/// Gets a value that indicates what operation was performed on the dragged data,
	/// and whether it was successful
	/// </summary>
	public DragDropEffects DropResult
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return _innerArgs.DropResult;
		}
	}

	/// <summary>
	/// Gets the item that was selected for the drag action
	/// </summary>
	public object Item { get; }

	/// <summary>
	/// Gets the TabViewItem that was selected for the drag action
	/// </summary>
	public FATabViewItem Tab { get; }

	internal FATabViewTabDragCompletedEventArgs(DragItemsCompletedEventArgs args, object item, FATabViewItem tab)
	{
		_innerArgs = args;
		Item = item;
		Tab = tab;
	}
}
