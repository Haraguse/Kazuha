using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Avalonia.Utilities;

internal static class RefCountable
{
	private class RefCounter
	{
		private IDisposable? _item;

		private volatile int _refs;

		internal int RefCount => _refs;

		public RefCounter(IDisposable item)
		{
			_item = item ?? throw new ArgumentNullException();
			_refs = 1;
		}

		internal bool TryAddRef()
		{
			int num = _refs;
			while (true)
			{
				if (num == 0)
				{
					return false;
				}
				int num2 = Interlocked.CompareExchange(ref _refs, num + 1, num);
				if (num2 == num)
				{
					break;
				}
				num = num2;
			}
			return true;
		}

		public void Release()
		{
			int num = _refs;
			while (true)
			{
				int num2 = Interlocked.CompareExchange(ref _refs, num - 1, num);
				if (num2 == num)
				{
					break;
				}
				num = num2;
			}
			if (num == 1)
			{
				_item?.Dispose();
				_item = null;
			}
		}
	}

	private class Ref<T> : CriticalFinalizerObject, IRef<T>, IDisposable where T : class
	{
		private volatile T? _item;

		private volatile RefCounter? _counter;

		public T Item => _item ?? throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">");

		public bool IsAlive => _item != null;

		public int RefCount => (_counter ?? throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">")).RefCount;

		public Ref(T item, RefCounter counter)
		{
			_item = item;
			_counter = counter;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		private void Dispose(bool disposing)
		{
			if (Interlocked.Exchange(ref _item, null) != null)
			{
				RefCounter? counter = _counter;
				_counter = null;
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
				counter.Release();
			}
		}

		~Ref()
		{
			Dispose(disposing: false);
		}

		public IRef<T> Clone()
		{
			RefCounter counter = _counter;
			T? item = _item;
			if (item == null || counter == null)
			{
				throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">");
			}
			if (!counter.TryAddRef())
			{
				throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">");
			}
			return new Ref<T>(item, counter);
		}

		public IRef<TResult> CloneAs<TResult>() where TResult : class
		{
			RefCounter counter = _counter;
			TResult obj = (TResult)(object)_item;
			if (obj == null || counter == null)
			{
				throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">");
			}
			if (!counter.TryAddRef())
			{
				throw new ObjectDisposedException("Ref<" + typeof(T)?.ToString() + ">");
			}
			return new Ref<TResult>(obj, counter);
		}
	}

	/// <summary>
	/// Create a reference counted object wrapping the given item.
	/// </summary>
	/// <typeparam name="T">The type of item.</typeparam>
	/// <param name="item">The item to refcount.</param>
	/// <returns>The refcounted reference to the item.</returns>
	public static IRef<T> Create<T>(T item) where T : class, IDisposable
	{
		return new Ref<T>(item, new RefCounter(item));
	}
}
