using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Utilities;

public struct ImmutableReadOnlyListStructEnumerator<T>(IReadOnlyList<T> readOnlyList) : IEnumerator<T>, IEnumerator, IDisposable
{
	private readonly IReadOnlyList<T> _readOnlyList = readOnlyList;

	private int _pos = -1;

	private T? _current = default(T);

	public T Current => _current;

	object? IEnumerator.Current => Current;

	public void Dispose()
	{
	}

	public bool MoveNext()
	{
		if (_pos >= _readOnlyList.Count - 1)
		{
			return false;
		}
		_current = _readOnlyList[++_pos];
		return true;
	}

	public void Reset()
	{
		_pos = -1;
		_current = default(T);
	}
}
