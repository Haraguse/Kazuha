using System;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls;

public class NameScopeLocator
{
	private class NeverEndingSynchronousCompletionAsyncResultObservable<T> : IObservable<T>
	{
		private T? _value;

		private SynchronousCompletionAsyncResult<T>? _asyncResult;

		public NeverEndingSynchronousCompletionAsyncResultObservable(SynchronousCompletionAsyncResult<T> task)
		{
			if (task.IsCompleted)
			{
				_value = task.GetResult();
			}
			else
			{
				_asyncResult = task;
			}
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			ref SynchronousCompletionAsyncResult<T>? asyncResult = ref _asyncResult;
			if (asyncResult.HasValue && asyncResult.GetValueOrDefault().IsCompleted)
			{
				_value = _asyncResult.Value.GetResult();
				_asyncResult = null;
			}
			if (_asyncResult.HasValue)
			{
				_asyncResult.Value.OnCompleted(delegate
				{
					observer.OnNext(_asyncResult.Value.GetResult());
				});
			}
			else
			{
				observer.OnNext(_value);
			}
			return Disposable.Empty;
		}
	}

	/// <summary>
	/// Tracks a named control relative to another control.
	/// </summary>
	/// <param name="scope">The scope relative from which the object should be resolved.</param>
	/// <param name="name">The name of the object to find.</param>
	public static IObservable<object?> Track(INameScope scope, string name)
	{
		return new NeverEndingSynchronousCompletionAsyncResultObservable<object>(scope.FindAsync(name));
	}
}
