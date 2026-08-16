using System;

namespace FluentAvalonia.UI.Controls;

internal struct WorkInfo(int priority, Action workFunc)
{
	private int _priority = priority;

	private Action _workFunc = workFunc;

	public int Priority => _priority;

	public void InvokeWorkFunc()
	{
		_workFunc();
	}
}
