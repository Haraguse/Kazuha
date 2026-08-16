using System;

namespace FluentAvalonia.Core;

internal class FADisposable : IDisposable
{
	private Action _dispose;

	public FADisposable(Action dispose)
	{
		_dispose = dispose;
	}

	public void Dispose()
	{
		_dispose();
	}
}
