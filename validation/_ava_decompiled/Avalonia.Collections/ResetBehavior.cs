namespace Avalonia.Collections;

/// <summary>
/// Describes the action notified on a clear of a <see cref="T:Avalonia.Collections.AvaloniaList`1" />.
/// </summary>
public enum ResetBehavior
{
	/// <summary>
	/// Clearing the list notifies with the <see cref="E:System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged" /> event with a
	/// <see cref="F:System.Collections.Specialized.NotifyCollectionChangedAction.Reset" /> action.
	/// </summary>
	Reset,
	/// <summary>
	/// Clearing the list notifies with the <see cref="E:System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged" /> event with a
	/// <see cref="F:System.Collections.Specialized.NotifyCollectionChangedAction.Remove" /> action.
	/// </summary>
	Remove
}
