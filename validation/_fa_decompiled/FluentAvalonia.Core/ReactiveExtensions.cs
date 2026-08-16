using System;

namespace FluentAvalonia.Core;

internal static class ReactiveExtensions
{
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

	public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> subAction)
	{
		return source.Subscribe(new SimpleObserver<T>(subAction));
	}

	public static IObservable<T> Skip<T>(this IObservable<T> source, int skipCount)
	{
		return Create(delegate(IObserver<T> obs)
		{
			int remaining = skipCount;
			return source.Subscribe(new SimpleObserver<T>(delegate(T input)
			{
				if (remaining <= 0)
				{
					obs.OnNext(input);
				}
				else
				{
					remaining--;
				}
			}));
		});
	}

	public static IObservable<TSource> Create<TSource>(Func<IObserver<TSource>, IDisposable> subscribe)
	{
		return new CreateWithDisposableObservable<TSource>(subscribe);
	}
}
