namespace Avalonia.LogicalTree;

/// <summary>
/// Describes the action that caused a <see cref="E:Avalonia.LogicalTree.IChildIndexProvider.ChildIndexChanged" /> event.
/// </summary>
public enum ChildIndexChangedAction
{
	/// <summary>
	/// The index of a single child changed.
	/// </summary>
	ChildIndexChanged,
	/// <summary>
	/// The index of multiple children changed and all children should be re-evaluated.
	/// </summary>
	ChildIndexesReset,
	/// <summary>
	/// The total number of children changed.
	/// </summary>
	TotalCountChanged
}
