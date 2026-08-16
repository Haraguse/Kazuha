using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.Reactive;

/// <summary>
/// Lightweight base class for observable implementations.
/// </summary>
/// <typeparam name="T">The observable type.</typeparam>
/// <remarks>
/// ObservableBase{T} is rather heavyweight in terms of allocations and memory
/// usage. This class provides a more lightweight base for some internal observable types
/// in the Avalonia framework.
/// </remarks>
internal abstract class LightweightObservableBase<T> : IObservable<T>
{
	private sealed class RemoveObserver : IDisposable
	{
		private LightweightObservableBase<T>? _parent;

		private IObserver<T>? _observer;

		public RemoveObserver(LightweightObservableBase<T> parent, IObserver<T> observer)
		{
			_parent = parent;
			Volatile.Write(ref _observer, observer);
		}

		public void Dispose()
		{
			IObserver<T> observer = _observer;
			Interlocked.Exchange(ref _parent, null)?.Remove(observer);
			_observer = null;
		}
	}

	private Exception? _error;

	private List<IObserver<T>>? _observers = new List<IObserver<T>>();

	public bool HasObservers
	{
		get
		{
			List<IObserver<T>>? observers = _observers;
			if (observers == null)
			{
				return false;
			}
			return observers.Count > 0;
		}
	}

	public IDisposable Subscribe(IObserver<T> observer)
	{
		if (observer == null)
		{
			throw new ArgumentNullException("observer");
		}
		bool flag = false;
		while (true)
		{
			if (Volatile.Read(in _observers) == null)
			{
				if (_error != null)
				{
					observer.OnError(_error);
				}
				else
				{
					observer.OnCompleted();
				}
				return Disposable.Empty;
			}
			lock (this)
			{
				if (_observers == null)
				{
					continue;
				}
				flag = _observers.Count == 0;
				_observers.Add(observer);
				break;
			}
		}
		if (flag)
		{
			Initialize();
		}
		Subscribed(observer, flag);
		return new RemoveObserver(this, observer);
	}

	private void Remove(IObserver<T> observer)
	{
		if (Volatile.Read(in _observers) == null)
		{
			return;
		}
		lock (this)
		{
			List<IObserver<T>> observers = _observers;
			if (observers != null)
			{
				observers.Remove(observer);
				if (observers.Count == 0)
				{
					observers.TrimExcess();
					Deinitialize();
				}
			}
		}
	}

	protected abstract void Initialize();

	protected abstract void Deinitialize();

	protected void PublishNext(T value)
	{
		if (Volatile.Read(in _observers) == null)
		{
			return;
		}
		IObserver<T>[] array = null;
		int num = 0;
		IObserver<T> observer = null;
		IObserver<T> observer2 = null;
		IObserver<T> observer3 = null;
		lock (this)
		{
			if (_observers == null)
			{
				return;
			}
			num = _observers.Count;
			switch (num)
			{
			case 3:
				observer = _observers[0];
				observer2 = _observers[1];
				observer3 = _observers[2];
				break;
			case 2:
				observer = _observers[0];
				observer2 = _observers[1];
				break;
			case 1:
				observer = _observers[0];
				break;
			case 0:
				return;
			default:
				array = ArrayPool<IObserver<T>>.Shared.Rent(num);
				_observers.CopyTo(array);
				break;
			}
		}
		if (observer != null)
		{
			observer.OnNext(value);
			observer2?.OnNext(value);
			observer3?.OnNext(value);
		}
		else if (array != null)
		{
			for (int i = 0; i < num; i++)
			{
				array[i].OnNext(value);
				array[i] = null;
			}
			ArrayPool<IObserver<T>>.Shared.Return(array);
		}
	}

	protected void PublishCompleted()
	{
		if (Volatile.Read(in _observers) == null)
		{
			return;
		}
		IObserver<T>[] array;
		lock (this)
		{
			if (_observers == null)
			{
				return;
			}
			array = _observers.ToArray();
			Volatile.Write(ref _observers, null);
		}
		IObserver<T>[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnCompleted();
		}
		Deinitialize();
	}

	protected void PublishError(Exception error)
	{
		if (Volatile.Read(in _observers) == null)
		{
			return;
		}
		IObserver<T>[] array;
		lock (this)
		{
			if (_observers == null)
			{
				return;
			}
			_error = error;
			array = _observers.ToArray();
			Volatile.Write(ref _observers, null);
		}
		IObserver<T>[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnError(error);
		}
		Deinitialize();
	}

	protected virtual void Subscribed(IObserver<T> observer, bool first)
	{
	}
}
