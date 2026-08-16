using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// A task-like operation that is guaranteed to finish continuations synchronously,
/// can be used for parametrized one-shot events
/// </summary>
public record struct SynchronousCompletionAsyncResult<T> : INotifyCompletion
{
	public bool IsCompleted
	{
		get
		{
			if (!_isValid)
			{
				ThrowNotInitialized();
			}
			if (_source != null)
			{
				return _source.IsCompleted;
			}
			return true;
		}
	}

	private readonly SynchronousCompletionAsyncResultSource<T>? _source;

	private readonly T? _result;

	private readonly bool _isValid;

	internal SynchronousCompletionAsyncResult(SynchronousCompletionAsyncResultSource<T> source)
	{
		_source = source;
		_result = default(T);
		_isValid = true;
	}

	public SynchronousCompletionAsyncResult(T result)
	{
		_result = result;
		_source = null;
		_isValid = true;
	}

	private static void ThrowNotInitialized()
	{
		throw new InvalidOperationException("This SynchronousCompletionAsyncResult was not initialized");
	}

	public T GetResult()
	{
		if (!_isValid)
		{
			ThrowNotInitialized();
		}
		if (_source != null)
		{
			return _source.Result;
		}
		return _result;
	}

	public void OnCompleted(Action continuation)
	{
		if (!_isValid)
		{
			ThrowNotInitialized();
		}
		if (_source == null)
		{
			continuation();
		}
		else
		{
			_source.OnCompleted(continuation);
		}
	}

	public SynchronousCompletionAsyncResult<T> GetAwaiter()
	{
		return this;
	}
}
