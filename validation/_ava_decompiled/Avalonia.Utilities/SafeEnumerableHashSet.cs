using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities;

/// <summary>
/// Implements a simple set which is safe to modify during enumeration.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// Implements a set which, when written to while enumerating, performs a copy of the set
/// items. Note this class doesn't actually implement <see cref="T:System.Collections.Generic.ISet`1" /> as it's not
/// currently needed - feel free to add missing methods etc.
/// </remarks>
internal class SafeEnumerableHashSet<T> : IEnumerable<T>, IEnumerable
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly SafeEnumerableHashSet<T> _owner;

		private readonly int _generation;

		private HashSet<T>.Enumerator _enumerator;

		public T Current => _enumerator.Current;

		object? IEnumerator.Current => _enumerator.Current;

		internal Enumerator(SafeEnumerableHashSet<T> owner, HashSet<T> list)
		{
			_owner = owner;
			_generation = owner._generation;
			_owner._enumCount++;
			_enumerator = list.GetEnumerator();
		}

		public void Dispose()
		{
			_enumerator.Dispose();
			if (_owner._generation == _generation)
			{
				_owner._enumCount--;
			}
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private HashSet<T> _hashSet = new HashSet<T>();

	private int _generation;

	private int _enumCount;

	public int Count => _hashSet.Count;

	internal HashSet<T> Inner => _hashSet;

	public void Add(T item)
	{
		GetSet().Add(item);
	}

	public bool Remove(T item)
	{
		return GetSet().Remove(item);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this, _hashSet);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private HashSet<T> GetSet()
	{
		if (_enumCount > 0)
		{
			_hashSet = new HashSet<T>(_hashSet);
			_generation++;
			_enumCount = 0;
		}
		return _hashSet;
	}
}
