using System;
using System.Collections.Generic;
using Avalonia.Reactive.Operators;

namespace Avalonia.Reactive;

/// <summary>
/// Provides common observable methods as a replacement for the Rx framework.
/// </summary>
internal static class Observable
{
	private sealed class SingleValueImpl<T> : IObservable<T>
	{
		private readonly T _value;

		public SingleValueImpl(T value)
		{
			_value = value;
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			observer.OnNext(_value);
			return Disposable.Empty;
		}
	}

	private sealed class ReturnImpl<T> : IObservable<T>
	{
		private readonly T _value;

		public ReturnImpl(T value)
		{
			_value = value;
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			observer.OnNext(_value);
			observer.OnCompleted();
			return Disposable.Empty;
		}
	}

	internal sealed class EmptyImpl<TResult> : IObservable<TResult>
	{
		internal static readonly IObservable<TResult> Instance = new EmptyImpl<TResult>();

		private EmptyImpl()
		{
		}

		public IDisposable Subscribe(IObserver<TResult> observer)
		{
			observer.OnCompleted();
			return Disposable.Empty;
		}
	}

	private sealed class CreateWithDisposableObservable<TSource> : IObservable<TSource>
	{
		private readonly Func<IObserver<TSource>, IDisposable> _subscribe;

		public CreateWithDisposableObservable(Func<IObserver<TSource>, IDisposable> subscribe)
		{
			_subscribe = subscribe;
		}

		public IDisposable Subscribe(IObserver<TSource> observer)
		{
			return _subscribe(observer);
		}
	}

	public static IObservable<TSource> Create<TSource>(Func<IObserver<TSource>, IDisposable> subscribe)
	{
		return new CreateWithDisposableObservable<TSource>(subscribe);
	}

	public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> action)
	{
		return source.Subscribe(new AnonymousObserver<T>(action));
	}

	public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
	{
		return Create((IObserver<TResult> obs) => source.Subscribe(new AnonymousObserver<TSource>(delegate(TSource input)
		{
			TResult value;
			try
			{
				value = selector(input);
			}
			catch (Exception error)
			{
				obs.OnError(error);
				return;
			}
			obs.OnNext(value);
		}, obs.OnError, obs.OnCompleted)));
	}

	public static IObservable<TSource> StartWith<TSource>(this IObservable<TSource> source, TSource value)
	{
		return Create(delegate(IObserver<TSource> obs)
		{
			obs.OnNext(value);
			return source.Subscribe(obs);
		});
	}

	public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
	{
		return Create((IObserver<TSource> obs) => source.Subscribe(new AnonymousObserver<TSource>(delegate(TSource input)
		{
			bool flag;
			try
			{
				flag = predicate(input);
			}
			catch (Exception error)
			{
				obs.OnError(error);
				return;
			}
			if (flag)
			{
				obs.OnNext(input);
			}
		}, obs.OnError, obs.OnCompleted)));
	}

	public static IObservable<TSource> Switch<TSource>(this IObservable<IObservable<TSource>> sources)
	{
		return new Switch<TSource>(sources);
	}

	public static IObservable<TResult> CombineLatest<TFirst, TSecond, TResult>(this IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		return new CombineLatest<TFirst, TSecond, TResult>(first, second, resultSelector);
	}

	public static IObservable<TInput[]> CombineLatest<TInput>(this IEnumerable<IObservable<TInput>> inputs)
	{
		return new CombineLatest<TInput, TInput[]>(inputs, (TInput[] items) => items);
	}

	public static IObservable<T> Skip<T>(this IObservable<T> source, int skipCount)
	{
		if (skipCount <= 0)
		{
			throw new ArgumentException("Skip count must be bigger than zero", "skipCount");
		}
		return Create(delegate(IObserver<T> obs)
		{
			int remaining = skipCount;
			return source.Subscribe(new AnonymousObserver<T>(delegate(T input)
			{
				if (remaining <= 0)
				{
					obs.OnNext(input);
				}
				else
				{
					remaining--;
				}
			}, obs.OnError, obs.OnCompleted));
		});
	}

	public static IObservable<T> Take<T>(this IObservable<T> source, int takeCount)
	{
		if (takeCount <= 0)
		{
			return Empty<T>();
		}
		return Create(delegate(IObserver<T> obs)
		{
			int remaining = takeCount;
			IDisposable sub = null;
			sub = source.Subscribe(new AnonymousObserver<T>(delegate(T input)
			{
				if (remaining > 0)
				{
					int num = remaining - 1;
					remaining = num;
					obs.OnNext(input);
					if (remaining == 0)
					{
						sub?.Dispose();
						obs.OnCompleted();
					}
				}
			}, obs.OnError, obs.OnCompleted));
			return sub;
		});
	}

	public static IObservable<EventArgs> FromEventPattern(Action<EventHandler> addHandler, Action<EventHandler> removeHandler)
	{
		return Create(delegate(IObserver<EventArgs> observer)
		{
			Action<EventArgs> handler = observer.OnNext;
			EventHandler converted = delegate(object? _, EventArgs args)
			{
				handler(args);
			};
			addHandler(converted);
			return Disposable.Create(delegate
			{
				removeHandler(converted);
			});
		});
	}

	public static IObservable<T> FromEventPattern<T>(Action<EventHandler<T>> addHandler, Action<EventHandler<T>> removeHandler) where T : EventArgs
	{
		return Create(delegate(IObserver<T> observer)
		{
			Action<T> handler = observer.OnNext;
			EventHandler<T> converted = delegate(object? _, T args)
			{
				handler(args);
			};
			addHandler(converted);
			return Disposable.Create(delegate
			{
				removeHandler(converted);
			});
		});
	}

	public static IObservable<T> Return<T>(T value)
	{
		return new ReturnImpl<T>(value);
	}

	public static IObservable<T> Empty<T>()
	{
		return EmptyImpl<T>.Instance;
	}

	/// <summary>
	/// Returns an observable that fires once with the specified value and never completes.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value.</param>
	/// <returns>The observable.</returns>
	public static IObservable<T> SingleValue<T>(T value)
	{
		return new SingleValueImpl<T>(value);
	}
}
