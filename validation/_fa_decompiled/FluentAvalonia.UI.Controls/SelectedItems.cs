using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FluentAvalonia.UI.Controls;

internal class SelectedItems<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	private class Iterator<TInner> : IEnumerator<TInner>, IEnumerator, IDisposable
	{
		private IReadOnlyList<TInner> _owner;

		private int _currentIndex;

		public TInner Current
		{
			get
			{
				IReadOnlyList<TInner> owner = _owner;
				if (_currentIndex < owner.Count)
				{
					return owner.ElementAt(_currentIndex);
				}
				return default(TInner);
			}
		}

		object IEnumerator.Current => Current;

		public Iterator(IReadOnlyList<TInner> owner)
		{
			_owner = owner;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (_currentIndex < _owner.Count)
			{
				_currentIndex++;
				return _currentIndex < _owner.Count;
			}
			return false;
		}

		public void Reset()
		{
		}
	}

	private IList<SelectedItemInfo> _infos;

	private int _totalCount;

	private Func<IList<SelectedItemInfo>, int, T> _getAtImpl;

	public int Count => _totalCount;

	public T this[int index] => GetAt(index);

	public SelectedItems(IList<SelectedItemInfo> infos, Func<IList<SelectedItemInfo>, int, T> getAtImpl)
	{
		_infos = infos;
		_getAtImpl = getAtImpl;
		foreach (SelectedItemInfo info in infos)
		{
			if (info.Node.TryGetTarget(out var target))
			{
				_totalCount += target.SelectedCount;
				continue;
			}
			throw new InvalidOperationException("Selection changed after the SelectedIndices/Items property was read");
		}
	}

	public int Size()
	{
		return Count;
	}

	public T GetAt(int index)
	{
		return _getAtImpl(_infos, index);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Iterator<T>(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
