using System;

namespace Avalonia.Diagnostics;

/// <summary>
/// Provides a debug interface into <see cref="T:System.Collections.Specialized.INotifyCollectionChanged" /> subscribers on
/// <see cref="T:Avalonia.Collections.AvaloniaList`1" />
/// </summary>
internal interface INotifyCollectionChangedDebug
{
	/// <summary>
	/// Gets the subscriber list for the <see cref="E:System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged" />
	/// event.
	/// </summary>
	/// <returns>
	/// The subscribers or null if no subscribers.
	/// </returns>
	Delegate[]? GetCollectionChangedSubscribers();
}
