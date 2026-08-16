using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Reactive;

internal sealed class CompositeDisposable : ICollection<IDisposable>, IEnumerable<IDisposable>, IEnumerable, IDisposable
{
	/// <summary>
	/// An enumerator for an array of disposables.
	/// </summary>
	private sealed class CompositeEnumerator : IEnumerator<IDisposable>, IEnumerator, IDisposable
	{
		private readonly IDisposable?[] _disposables;

		private int _index;

		public IDisposable Current => _disposables[_index];

		object IEnumerator.Current => _disposables[_index];

		public CompositeEnumerator(IDisposable?[] disposables)
		{
			_disposables = disposables;
			_index = -1;
		}

		public void Dispose()
		{
			IDisposable[] disposables = _disposables;
			Array.Clear(disposables, 0, disposables.Length);
		}

		public bool MoveNext()
		{
			IDisposable[] disposables = _disposables;
			int num;
			do
			{
				num = ++_index;
				if (num >= disposables.Length)
				{
					return false;
				}
			}
			while (disposables[num] == null);
			return true;
		}

		public void Reset()
		{
			_index = -1;
		}
	}

	private readonly object _gate = new object();

	private bool _disposed;

	private List<IDisposable?> _disposables;

	private int _count;

	private const int ShrinkThreshold = 64;

	/// <summary>
	/// An empty enumerator for the <see cref="M:Avalonia.Reactive.CompositeDisposable.GetEnumerator" />
	/// method to avoid allocation on disposed or empty composites.
	/// </summary>
	private static readonly CompositeEnumerator EmptyEnumerator = new CompositeEnumerator(Array.Empty<IDisposable>());

	/// <summary>
	/// Gets the number of disposables contained in the <see cref="T:Avalonia.Reactive.CompositeDisposable" />.
	/// </summary>
	public int Count => Volatile.Read(in _count);

	/// <summary>
	/// Always returns false.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Gets a value that indicates whether the object is disposed.
	/// </summary>
	public bool IsDisposed => Volatile.Read(in _disposed);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> class with the specified number of disposables.
	/// </summary>
	/// <param name="capacity">The number of disposables that the new CompositeDisposable can initially store.</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
	public CompositeDisposable(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity");
		}
		_disposables = new List<IDisposable>(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> class from a group of disposables.
	/// </summary>
	/// <param name="disposables">Disposables that will be disposed together.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="disposables" /> is <c>null</c>.</exception>
	/// <exception cref="T:System.ArgumentException">Any of the disposables in the <paramref name="disposables" /> collection is <c>null</c>.</exception>
	public CompositeDisposable(params IDisposable[] disposables)
	{
		if (disposables == null)
		{
			throw new ArgumentNullException("disposables");
		}
		_disposables = ToList(disposables);
		Volatile.Write(ref _count, _disposables.Count);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> class from a group of disposables.
	/// </summary>
	/// <param name="disposables">Disposables that will be disposed together.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="disposables" /> is <c>null</c>.</exception>
	/// <exception cref="T:System.ArgumentException">Any of the disposables in the <paramref name="disposables" /> collection is <c>null</c>.</exception>
	public CompositeDisposable(IList<IDisposable> disposables)
	{
		if (disposables == null)
		{
			throw new ArgumentNullException("disposables");
		}
		_disposables = ToList(disposables);
		Volatile.Write(ref _count, _disposables.Count);
	}

	private static List<IDisposable?> ToList(IEnumerable<IDisposable> disposables)
	{
		int capacity = ((disposables is IDisposable[] array) ? array.Length : ((!(disposables is ICollection<IDisposable> collection)) ? 12 : collection.Count));
		List<IDisposable> list = new List<IDisposable>(capacity);
		foreach (IDisposable disposable in disposables)
		{
			if (disposable == null)
			{
				throw new ArgumentException("Disposables can't contain null", "disposables");
			}
			list.Add(disposable);
		}
		return list;
	}

	/// <summary>
	/// Adds a disposable to the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> or disposes the disposable if the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> is disposed.
	/// </summary>
	/// <param name="item">Disposable to add.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="item" /> is <c>null</c>.</exception>
	public void Add(IDisposable item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		lock (_gate)
		{
			if (!_disposed)
			{
				_disposables.Add(item);
				Volatile.Write(ref _count, _count + 1);
				return;
			}
		}
		item.Dispose();
	}

	/// <summary>
	/// Removes and disposes the first occurrence of a disposable from the <see cref="T:Avalonia.Reactive.CompositeDisposable" />.
	/// </summary>
	/// <param name="item">Disposable to remove.</param>
	/// <returns>true if found; false otherwise.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="item" /> is <c>null</c>.</exception>
	public bool Remove(IDisposable item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		lock (_gate)
		{
			if (_disposed)
			{
				return false;
			}
			List<IDisposable> disposables = _disposables;
			int num = disposables.IndexOf(item);
			if (num < 0)
			{
				return false;
			}
			disposables[num] = null;
			if (disposables.Capacity > 64 && _count < disposables.Capacity / 2)
			{
				List<IDisposable> list = new List<IDisposable>(disposables.Capacity / 2);
				foreach (IDisposable item2 in disposables)
				{
					if (item2 != null)
					{
						list.Add(item2);
					}
				}
				_disposables = list;
			}
			Volatile.Write(ref _count, _count - 1);
		}
		item.Dispose();
		return true;
	}

	/// <summary>
	/// Disposes all disposables in the group and removes them from the group.
	/// </summary>
	public void Dispose()
	{
		List<IDisposable> list = null;
		lock (_gate)
		{
			if (!_disposed)
			{
				list = _disposables;
				_disposables = null;
				Volatile.Write(ref _count, 0);
				Volatile.Write(ref _disposed, value: true);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (IDisposable item in list)
		{
			item?.Dispose();
		}
	}

	/// <summary>
	/// Removes and disposes all disposables from the <see cref="T:Avalonia.Reactive.CompositeDisposable" />, but does not dispose the <see cref="T:Avalonia.Reactive.CompositeDisposable" />.
	/// </summary>
	public void Clear()
	{
		IDisposable[] array;
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			List<IDisposable?> disposables = _disposables;
			array = disposables.ToArray();
			disposables.Clear();
			Volatile.Write(ref _count, 0);
		}
		IDisposable[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i]?.Dispose();
		}
	}

	/// <summary>
	/// Determines whether the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> contains a specific disposable.
	/// </summary>
	/// <param name="item">Disposable to search for.</param>
	/// <returns>true if the disposable was found; otherwise, false.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="item" /> is <c>null</c>.</exception>
	public bool Contains(IDisposable item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		lock (_gate)
		{
			if (_disposed)
			{
				return false;
			}
			return _disposables.Contains(item);
		}
	}

	/// <summary>
	/// Copies the disposables contained in the <see cref="T:Avalonia.Reactive.CompositeDisposable" /> to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">Array to copy the contained disposables to.</param>
	/// <param name="arrayIndex">Target index at which to copy the first disposable of the group.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is <c>null</c>.</exception>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than zero. -or - <paramref name="arrayIndex" /> is larger than or equal to the array length.</exception>
	public void CopyTo(IDisposable[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (arrayIndex < 0 || arrayIndex >= array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex");
		}
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			if (arrayIndex + _count > array.Length)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			int num = arrayIndex;
			foreach (IDisposable disposable in _disposables)
			{
				if (disposable != null)
				{
					array[num++] = disposable;
				}
			}
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the <see cref="T:Avalonia.Reactive.CompositeDisposable" />.
	/// </summary>
	/// <returns>An enumerator to iterate over the disposables.</returns>
	public IEnumerator<IDisposable> GetEnumerator()
	{
		lock (_gate)
		{
			if (_disposed || _count == 0)
			{
				return EmptyEnumerator;
			}
			return new CompositeEnumerator(_disposables.ToArray());
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the <see cref="T:Avalonia.Reactive.CompositeDisposable" />.
	/// </summary>
	/// <returns>An enumerator to iterate over the disposables.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
