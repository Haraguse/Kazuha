using System;
using System.Collections;
using System.Collections.Generic;

namespace FluentAvalonia.Core;

internal class FACompositeDisposable : ICollection<IDisposable>, IEnumerable<IDisposable>, IEnumerable, IDisposable
{
	private readonly List<IDisposable> _list;

	public int Count => _list.Count;

	bool ICollection<IDisposable>.IsReadOnly => false;

	public FACompositeDisposable()
	{
		_list = new List<IDisposable>();
	}

	public FACompositeDisposable(int capacity)
	{
		_list = new List<IDisposable>(capacity);
	}

	public FACompositeDisposable(params IDisposable[] disposables)
	{
		_list = new List<IDisposable>(disposables);
	}

	public FACompositeDisposable(IEnumerable<IDisposable> disposables)
	{
		_list = new List<IDisposable>(disposables);
	}

	public void Add(IDisposable item)
	{
		_list.Add(item);
	}

	public void Clear()
	{
		Dispose();
	}

	public bool Contains(IDisposable item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(IDisposable[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public void Dispose()
	{
		for (int num = _list.Count - 1; num >= 0; num--)
		{
			_list[num].Dispose();
			_list.RemoveAt(num);
		}
	}

	public IEnumerator<IDisposable> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	public bool Remove(IDisposable item)
	{
		int num = _list.IndexOf(item);
		if (num == -1)
		{
			return false;
		}
		_list.RemoveAt(num);
		item.Dispose();
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}
}
