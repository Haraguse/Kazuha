using System;

namespace FluentAvalonia.UI.Data;

internal class RefreshDeferer : IDisposable
{
	private readonly Action<object> _releaseAction;

	private readonly object _currentItem;

	public RefreshDeferer(Action<object> release, object currentItem)
	{
		_currentItem = currentItem;
		_releaseAction = release;
	}

	public void Dispose()
	{
		_releaseAction(_currentItem);
	}
}
