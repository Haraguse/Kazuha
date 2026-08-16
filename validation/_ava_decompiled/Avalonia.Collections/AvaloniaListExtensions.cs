using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Reactive;

namespace Avalonia.Collections;

/// <summary>
/// Defines extension methods for working with <see cref="T:Avalonia.Collections.AvaloniaList`1" />s.
/// </summary>
public static class AvaloniaListExtensions
{
	/// <summary>
	/// Invokes an action for each item in a collection and subsequently each item added or
	/// removed from the collection.
	/// </summary>
	/// <typeparam name="T">The type of the collection items.</typeparam>
	/// <param name="collection">The collection.</param>
	/// <param name="added">
	/// An action called initially for each item in the collection and subsequently for each
	/// item added to the collection. The parameters passed are the index in the collection and
	/// the item.
	/// </param>
	/// <param name="removed">
	/// An action called for each item removed from the collection. The parameters passed are
	/// the index in the collection and the item.
	/// </param>
	/// <param name="reset">
	/// An action called when the collection is reset.
	/// </param>
	/// <param name="weakSubscription">
	/// Indicates if a weak subscription should be used to track changes to the collection.
	/// </param>
	/// <returns>A disposable used to terminate the subscription.</returns>
	public static IDisposable ForEachItem<T>(this IAvaloniaReadOnlyList<T> collection, Action<T> added, Action<T> removed, Action reset, bool weakSubscription = false)
	{
		return collection.ForEachItem(delegate(int _, T i)
		{
			added(i);
		}, delegate(int _, T i)
		{
			removed(i);
		}, reset, weakSubscription);
	}

	/// <summary>
	/// Invokes an action for each item in a collection and subsequently each item added or
	/// removed from the collection.
	/// </summary>
	/// <typeparam name="T">The type of the collection items.</typeparam>
	/// <param name="collection">The collection.</param>
	/// <param name="added">
	/// An action called initially for each item in the collection and subsequently for each
	/// item added to the collection. The parameters passed are the index in the collection and
	/// the item.
	/// </param>
	/// <param name="removed">
	/// An action called for each item removed from the collection. The parameters passed are
	/// the index in the collection and the item.
	/// </param>
	/// <param name="reset">
	/// An action called when the collection is reset. This will be followed by calls to 
	/// <paramref name="added" /> for each item present in the collection after the reset.
	/// </param>
	/// <param name="weakSubscription">
	/// Indicates if a weak subscription should be used to track changes to the collection.
	/// </param>
	/// <returns>A disposable used to terminate the subscription.</returns>
	public static IDisposable ForEachItem<T>(this IAvaloniaReadOnlyList<T> collection, Action<int, T> added, Action<int, T> removed, Action reset, bool weakSubscription = false)
	{
		NotifyCollectionChangedEventHandler handler = delegate(object? _, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			default:
				return;
			case NotifyCollectionChangedAction.Add:
				Add(e.NewStartingIndex, e.NewItems);
				return;
			case NotifyCollectionChangedAction.Move:
				if (e.OldStartingIndex >= 0)
				{
					Remove(e.OldStartingIndex, e.OldItems);
					int num = e.NewStartingIndex;
					if (num > e.OldStartingIndex)
					{
						num -= e.OldItems.Count - 1;
					}
					Add(num, e.NewItems);
					return;
				}
				break;
			case NotifyCollectionChangedAction.Replace:
				if (e.OldStartingIndex >= 0)
				{
					Remove(e.OldStartingIndex, e.OldItems);
					Add(e.NewStartingIndex, e.NewItems);
					return;
				}
				break;
			case NotifyCollectionChangedAction.Remove:
				Remove(e.OldStartingIndex, e.OldItems);
				return;
			case NotifyCollectionChangedAction.Reset:
				break;
			}
			if (reset == null)
			{
				throw new InvalidOperationException("Reset called on collection without reset handler.");
			}
			reset();
			Add(0, (IList)collection);
		};
		Add(0, (IList)collection);
		if (weakSubscription)
		{
			return collection.WeakSubscribe(handler);
		}
		collection.CollectionChanged += handler;
		return Disposable.Create(delegate
		{
			collection.CollectionChanged -= handler;
		});
		void Add(int index, IList items)
		{
			foreach (T item in items)
			{
				added(index++, item);
			}
		}
		void Remove(int index, IList items)
		{
			for (int num = items.Count - 1; num >= 0; num--)
			{
				removed(index + num, (T)items[num]);
			}
		}
	}

	/// <summary>
	/// Listens for property changed events from all items in a collection.
	/// </summary>
	/// <typeparam name="T">The type of the collection items.</typeparam>
	/// <param name="collection">The collection.</param>
	/// <param name="callback">A callback to call for each property changed event.</param>
	/// <returns>A disposable used to terminate the subscription.</returns>
	public static IDisposable TrackItemPropertyChanged<T>(this IAvaloniaReadOnlyList<T> collection, Action<Tuple<object?, PropertyChangedEventArgs>> callback)
	{
		List<INotifyPropertyChanged> tracked = new List<INotifyPropertyChanged>();
		PropertyChangedEventHandler handler = delegate(object? s, PropertyChangedEventArgs e)
		{
			callback(Tuple.Create(s, e));
		};
		collection.ForEachItem(delegate(T x)
		{
			if (x is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged += handler;
				tracked.Add(notifyPropertyChanged);
			}
		}, delegate(T x)
		{
			if (x is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged -= handler;
				tracked.Remove(notifyPropertyChanged);
			}
		}, delegate
		{
			throw new NotSupportedException("Collection reset not supported.");
		});
		return Disposable.Create(delegate
		{
			foreach (INotifyPropertyChanged item in tracked)
			{
				item.PropertyChanged -= handler;
			}
		});
	}
}
