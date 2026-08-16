using System;
using System.Threading;

namespace Avalonia.Platform;

public class ScopedResource<T> : IScopedResource<T>, IDisposable
{
	private int _disposed;

	private T _value;

	private Action? _dispose;

	public T Value
	{
		get
		{
			if (_disposed == 1)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			return _value;
		}
	}

	private ScopedResource(T value, Action dispose)
	{
		_value = value;
		_dispose = dispose;
	}

	public static IScopedResource<T> Create(T value, Action dispose)
	{
		return new ScopedResource<T>(value, dispose);
	}

	public void Dispose()
	{
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
		{
			Action? dispose = _dispose;
			_value = default(T);
			_dispose = null;
			dispose();
		}
	}
}
