using System;
using System.Collections.Generic;
using Avalonia.Threading;

namespace FluentAvalonia.UI.Controls;

internal static class BuildTreeScheduler
{
	private static double _budgetInMs = 40.0;

	private static readonly object _lockObj = new object();

	[ThreadStatic]
	private static QPCTimer _timer = new QPCTimer();

	[ThreadStatic]
	private static readonly List<WorkInfo> _pendingWork = new List<WorkInfo>();

	private static bool _renderingToken;

	public static void RegisterWork(int priority, Action workFunc)
	{
		if (priority < 0)
		{
			throw new ArgumentOutOfRangeException("priority", "Priority must be >= 0");
		}
		if (workFunc == null)
		{
			throw new ArgumentNullException("workFunc");
		}
		QueueTick();
		_pendingWork.Add(new WorkInfo(priority, workFunc));
	}

	public static bool ShouldYield()
	{
		return (double)_timer.DurationInMilliseconds() > _budgetInMs;
	}

	public static void OnRendering()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldYield() && _pendingWork.Count > 0)
		{
			_pendingWork.Sort((WorkInfo x, WorkInfo y) => (x.Priority > y.Priority) ? 1 : (-1));
			int num = _pendingWork.Count - 1;
			do
			{
				_pendingWork[num].InvokeWorkFunc();
				_pendingWork.RemoveAt(num);
			}
			while (--num >= 0 && !ShouldYield());
		}
		if (_pendingWork.Count == 0)
		{
			_renderingToken = false;
		}
		_timer.Reset();
		if (_renderingToken)
		{
			Dispatcher.UIThread.Post((Action)OnRendering, DispatcherPriority.Render);
		}
	}

	public static void QueueTick()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!_renderingToken)
		{
			_renderingToken = true;
			Dispatcher.UIThread.Post((Action)OnRendering, DispatcherPriority.Render);
		}
	}
}
