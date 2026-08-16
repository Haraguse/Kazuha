using System;

namespace Avalonia.LogicalTree;

/// <summary>
/// Event args for <see cref="E:Avalonia.LogicalTree.IChildIndexProvider.ChildIndexChanged" /> event.
/// </summary>
public class ChildIndexChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the type of change action that ocurred on the list control.
	/// </summary>
	public ChildIndexChangedAction Action { get; }

	/// <summary>
	/// Gets the logical child whose index was changed or null if all children should be re-evaluated.
	/// </summary>
	public ILogical? Child { get; }

	/// <summary>
	/// Gets the new index of <see cref="P:Avalonia.LogicalTree.ChildIndexChangedEventArgs.Child" /> or -1 if all children should be re-evaluated.
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Gets an instance of the <see cref="T:Avalonia.LogicalTree.ChildIndexChangedEventArgs" /> with an action of
	/// <see cref="F:Avalonia.LogicalTree.ChildIndexChangedAction.ChildIndexesReset" />.
	/// </summary>
	public static ChildIndexChangedEventArgs ChildIndexesReset { get; } = new ChildIndexChangedEventArgs(ChildIndexChangedAction.ChildIndexesReset);

	/// <summary>
	/// Gets an instance of the <see cref="T:Avalonia.LogicalTree.ChildIndexChangedEventArgs" /> with an action of
	/// <see cref="F:Avalonia.LogicalTree.ChildIndexChangedAction.TotalCountChanged" />.
	/// </summary>
	public static ChildIndexChangedEventArgs TotalCountChanged { get; } = new ChildIndexChangedEventArgs(ChildIndexChangedAction.TotalCountChanged);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.LogicalTree.ChildIndexChangedEventArgs" /> class with
	/// an action of <see cref="F:Avalonia.LogicalTree.ChildIndexChangedAction.ChildIndexChanged" />.
	/// </summary>
	/// <param name="child">The child whose index was changed.</param>
	/// <param name="index">The new index of the child.</param>
	public ChildIndexChangedEventArgs(ILogical child, int index)
	{
		Action = ChildIndexChangedAction.ChildIndexChanged;
		Child = child;
		Index = index;
	}

	private ChildIndexChangedEventArgs(ChildIndexChangedAction action)
	{
		Action = action;
		Index = -1;
	}
}
