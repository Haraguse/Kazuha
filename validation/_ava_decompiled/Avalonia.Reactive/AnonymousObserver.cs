using System;
using System.Threading.Tasks;

namespace Avalonia.Reactive;

/// <summary>
/// Class to create an <see cref="T:System.IObserver`1" /> instance from delegate-based implementations of the On* methods.
/// </summary>
/// <typeparam name="T">The type of the elements in the sequence.</typeparam>
public class AnonymousObserver<T> : IObserver<T>
{
	private readonly Action<T> _onNext;

	private readonly Action<Exception> _onError;

	private readonly Action _onCompleted;

	public AnonymousObserver(TaskCompletionSource<T> tcs)
	{
		if (tcs == null)
		{
			throw new ArgumentNullException("tcs");
		}
		_onNext = tcs.SetResult;
		_onError = tcs.SetException;
		_onCompleted = AnonymousObserverNonGenericHelper.NoOpCompleted;
	}

	public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
	{
		_onNext = onNext ?? throw new ArgumentNullException("onNext");
		_onError = onError ?? throw new ArgumentNullException("onError");
		_onCompleted = onCompleted ?? throw new ArgumentNullException("onCompleted");
	}

	public AnonymousObserver(Action<T> onNext)
		: this(onNext, AnonymousObserverNonGenericHelper.ThrowsOnError, AnonymousObserverNonGenericHelper.NoOpCompleted)
	{
	}

	public AnonymousObserver(Action<T> onNext, Action<Exception> onError)
		: this(onNext, onError, AnonymousObserverNonGenericHelper.NoOpCompleted)
	{
	}

	public AnonymousObserver(Action<T> onNext, Action onCompleted)
		: this(onNext, AnonymousObserverNonGenericHelper.ThrowsOnError, onCompleted)
	{
	}

	public void OnCompleted()
	{
		_onCompleted();
	}

	public void OnError(Exception error)
	{
		_onError(error);
	}

	public void OnNext(T value)
	{
		_onNext(value);
	}
}
