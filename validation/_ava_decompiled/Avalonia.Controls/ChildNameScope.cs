using Avalonia.Utilities;

namespace Avalonia.Controls;

internal class ChildNameScope : INameScope
{
	private readonly INameScope _parentScope;

	private readonly NameScope _inner = new NameScope();

	public bool IsCompleted
	{
		get
		{
			if (_inner.IsCompleted)
			{
				return _parentScope.IsCompleted;
			}
			return false;
		}
	}

	public ChildNameScope(INameScope parentScope)
	{
		_parentScope = parentScope;
	}

	public void Register(string name, object element)
	{
		_inner.Register(name, element);
	}

	public SynchronousCompletionAsyncResult<object?> FindAsync(string name)
	{
		object obj = Find(name);
		if (obj != null)
		{
			return new SynchronousCompletionAsyncResult<object>(obj);
		}
		if (IsCompleted)
		{
			return new SynchronousCompletionAsyncResult<object>((object)null);
		}
		return DoFindAsync(name);
	}

	public SynchronousCompletionAsyncResult<object?> DoFindAsync(string name)
	{
		SynchronousCompletionAsyncResultSource<object?> src = new SynchronousCompletionAsyncResultSource<object>();
		if (!_inner.IsCompleted)
		{
			SynchronousCompletionAsyncResult<object?> innerSearch = _inner.FindAsync(name);
			innerSearch.OnCompleted(delegate
			{
				object result = innerSearch.GetResult();
				if (result != null)
				{
					src.SetResult(result);
				}
				else
				{
					ParentSearch();
				}
			});
		}
		else
		{
			ParentSearch();
		}
		return src.AsyncResult;
		void ParentSearch()
		{
			SynchronousCompletionAsyncResult<object?> parentSearch = _parentScope.FindAsync(name);
			if (parentSearch.IsCompleted)
			{
				src.SetResult(parentSearch.GetResult());
			}
			else
			{
				parentSearch.OnCompleted(delegate
				{
					src.SetResult(parentSearch.GetResult());
				});
			}
		}
	}

	public object? Find(string name)
	{
		object obj = _inner.Find(name);
		if (obj != null)
		{
			return obj;
		}
		if (_inner.IsCompleted)
		{
			return _parentScope.Find(name);
		}
		return null;
	}

	public void Complete()
	{
		_inner.Complete();
	}
}
