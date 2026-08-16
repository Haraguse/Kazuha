using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a standardized view of the supported interactions between a given ItemsSource object and an ItemsRepeater control.
/// </summary>
public class FAItemsSourceView
{
	private int _cachedSize = -1;

	private IEnumerable _vector;

	private IFAKeyIndexMapping _uniqueIdMapping;

	private IDisposable _eventToken;

	/// <summary>
	/// Gets the number of items in the collection.
	/// </summary>
	public int Count
	{
		get
		{
			if (_cachedSize == -1)
			{
				_cachedSize = GetSizeCore();
			}
			return _cachedSize;
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the items source can provide a unique key for each item.
	/// </summary>
	public bool HasKeyIndexMapping => HasKeyIndexMappingCore();

	/// <summary>
	/// Occurs when the collection has changed to indicate the reason for the change and which items changed.
	/// </summary>
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public FAItemsSourceView(IEnumerable source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		_vector = source;
		ListenToCollectionChanges();
		_uniqueIdMapping = source as IFAKeyIndexMapping;
	}

	/// <summary>
	/// Retrieves the item at the specified index.
	/// </summary>
	public object GetAt(int index)
	{
		return GetAtCore(index);
	}

	/// <summary>
	/// Retrieves the index of the item that has the specified unique identifier (key).
	/// </summary>
	public string KeyFromIndex(int index)
	{
		return KeyFromIndexCore(index);
	}

	/// <summary>
	/// Retrieves the index of the item that has the specified unique identifier (key).
	/// </summary>
	public int IndexFromKey(string id)
	{
		return IndexFromKeyCore(id);
	}

	/// <summary>
	/// Retrieves the index of the specified item.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public int IndexOf(object value)
	{
		return IndexOfCore(value);
	}

	/// <summary>
	/// Called when the ItemsSource has raised a CollectionChanged event
	/// </summary>
	/// <param name="args"></param>
	protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
	{
		_cachedSize = GetSizeCore();
		CollectionChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Gets the count of the underlying collection
	/// </summary>
	protected virtual int GetSizeCore()
	{
		if (_vector is IList list)
		{
			return list.Count;
		}
		return _vector.Count();
	}

	/// <summary>
	/// Gets the item at the specified index from the underlying collection
	/// </summary>
	protected virtual object GetAtCore(int index)
	{
		if (_vector is IList list)
		{
			return list[index];
		}
		return _vector.ElementAt(index);
	}

	/// <summary>
	/// Gets whether this underlying supports Key-Index mapping
	/// </summary>
	/// <returns></returns>
	protected virtual bool HasKeyIndexMappingCore()
	{
		return _uniqueIdMapping != null;
	}

	/// <summary>
	/// Gets the key from the specified index
	/// </summary>
	protected string KeyFromIndexCore(int index)
	{
		if (_uniqueIdMapping != null)
		{
			return _uniqueIdMapping.KeyFromIndex(index);
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets the Index from the specified key
	/// </summary>
	protected virtual int IndexFromKeyCore(string id)
	{
		if (_uniqueIdMapping != null)
		{
			return _uniqueIdMapping.IndexFromKey(id);
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Queries the underlying collection for the item at the specified index
	/// </summary>
	protected virtual int IndexOfCore(object value)
	{
		int num = -1;
		if (_vector is IList list)
		{
			return list.IndexOf(value);
		}
		return _vector.IndexOf(value);
	}

	private void UnListenToCollectionChanges()
	{
		_eventToken?.Dispose();
		_eventToken = null;
	}

	private void ListenToCollectionChanges()
	{
		if (_vector == null)
		{
			throw new Exception("No source attached");
		}
		if (_vector is INotifyCollectionChanged notifyCollectionChanged)
		{
			_eventToken = NotifyCollectionChangedExtensions.GetWeakCollectionChangedObservable(notifyCollectionChanged).Subscribe(new SimpleObserver<NotifyCollectionChangedEventArgs>(OnCollectionChanged));
		}
	}

	private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		OnItemsSourceChanged(args);
	}
}
