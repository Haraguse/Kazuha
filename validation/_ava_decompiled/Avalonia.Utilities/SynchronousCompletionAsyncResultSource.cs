using System;
using System.Collections.Generic;

namespace Avalonia.Utilities;

/// <summary>
/// Source for incomplete SynchronousCompletionAsyncResult
/// </summary>
/// <typeparam name="T"></typeparam>
public class SynchronousCompletionAsyncResultSource<T>
{
	private T? _result;

	private List<Action>? _continuations;

	internal bool IsCompleted { get; private set; }

	public SynchronousCompletionAsyncResult<T> AsyncResult => new SynchronousCompletionAsyncResult<T>(this);

	internal T Result
	{
		get
		{
			if (!IsCompleted)
			{
				throw new InvalidOperationException("Asynchronous operation is not yet completed");
			}
			return _result;
		}
	}

	internal void OnCompleted(Action continuation)
	{
		if (_continuations == null)
		{
			_continuations = new List<Action>();
		}
		_continuations.Add(continuation);
	}

	public void SetResult(T result)
	{
		if (IsCompleted)
		{
			throw new InvalidOperationException("Asynchronous operation is already completed");
		}
		_result = result;
		IsCompleted = true;
		if (_continuations != null)
		{
			foreach (Action continuation in _continuations)
			{
				continuation();
			}
		}
		_continuations = null;
	}

	public void TrySetResult(T result)
	{
		if (!IsCompleted)
		{
			SetResult(result);
		}
	}
}
