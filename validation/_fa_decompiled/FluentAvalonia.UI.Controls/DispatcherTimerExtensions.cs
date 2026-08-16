using System;
using System.Collections.Concurrent;
using Avalonia.Threading;

namespace FluentAvalonia.UI.Controls;

internal static class DispatcherTimerExtensions
{
	private static ConcurrentDictionary<DispatcherTimer, Action> _debounceInstances = new ConcurrentDictionary<DispatcherTimer, Action>();

	public static void Debounce(this DispatcherTimer timer, Action action, TimeSpan interval, bool immediate = false)
	{
		bool isEnabled = timer.IsEnabled;
		if (isEnabled)
		{
			timer.Stop();
		}
		timer.Tick -= TimerTick;
		timer.Interval = interval;
		if (immediate)
		{
			if (!isEnabled)
			{
				action();
			}
		}
		else
		{
			timer.Tick += TimerTick;
			_debounceInstances.AddOrUpdate(timer, action, (DispatcherTimer k, Action v) => action);
		}
		timer.Start();
	}

	private static void TimerTick(object sender, object e)
	{
		DispatcherTimer val = (DispatcherTimer)((sender is DispatcherTimer) ? sender : null);
		if (val != null)
		{
			val.Tick -= TimerTick;
			val.Stop();
			if (_debounceInstances.TryRemove(val, out var value))
			{
				value?.Invoke();
			}
		}
	}
}
