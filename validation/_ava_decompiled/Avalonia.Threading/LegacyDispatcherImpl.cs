using System;
using System.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Threading;

internal class LegacyDispatcherImpl : IDispatcherImpl
{
	private readonly IPlatformThreadingInterface _platformThreading;

	private IDisposable? _timer;

	private readonly Stopwatch _clock = Stopwatch.StartNew();

	public bool CurrentThreadIsLoopThread => _platformThreading.CurrentThreadIsLoopThread;

	public long Now => _clock.ElapsedMilliseconds;

	public event Action? Signaled;

	public event Action? Timer;

	public LegacyDispatcherImpl(IPlatformThreadingInterface platformThreading)
	{
		_platformThreading = platformThreading;
		_platformThreading.Signaled += delegate
		{
			Signaled?.Invoke();
		};
	}

	public void Signal()
	{
		_platformThreading.Signal(DispatcherPriority.Send);
	}

	public void UpdateTimer(long? dueTimeInMs)
	{
		_timer?.Dispose();
		_timer = null;
		if (dueTimeInMs.HasValue)
		{
			long milliseconds = Math.Max(1L, dueTimeInMs.Value - _clock.ElapsedMilliseconds);
			_timer = _platformThreading.StartTimer(DispatcherPriority.Send, TimeSpan.FromMilliseconds(milliseconds), OnTick);
		}
	}

	private void OnTick()
	{
		_timer?.Dispose();
		_timer = null;
		Timer?.Invoke();
	}
}
