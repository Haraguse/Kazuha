using System;
using System.Threading;

namespace Avalonia.Utilities;

internal class DisposableLock
{
	private sealed class UnlockDisposable : IDisposable
	{
		private object? _lock;

		public UnlockDisposable(object @lock)
		{
			_lock = @lock;
		}

		public void Dispose()
		{
			object obj = Interlocked.Exchange(ref _lock, null);
			if (obj != null)
			{
				Monitor.Exit(obj);
			}
		}
	}

	private readonly object _lock = new object();

	/// <summary>
	/// Tries to take a lock
	/// </summary>
	/// <returns>IDisposable if succeeded to obtain the lock</returns>
	public IDisposable? TryLock()
	{
		if (Monitor.TryEnter(_lock))
		{
			return new UnlockDisposable(_lock);
		}
		return null;
	}

	/// <summary>
	/// Enters a waiting lock
	/// </summary>
	public IDisposable Lock()
	{
		Monitor.Enter(_lock);
		return new UnlockDisposable(_lock);
	}
}
