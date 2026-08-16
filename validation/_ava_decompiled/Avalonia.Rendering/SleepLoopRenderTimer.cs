using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering;

[PrivateApi]
public class SleepLoopRenderTimer : IRenderTimer
{
	private volatile Action<TimeSpan>? _tick;

	private volatile bool _stopped = true;

	private bool _threadStarted;

	private readonly AutoResetEvent _wakeEvent = new AutoResetEvent(initialState: false);

	private readonly Stopwatch _st = Stopwatch.StartNew();

	private volatile int _desiredFps;

	public int DesiredFps
	{
		get
		{
			return _desiredFps;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, "value");
			_desiredFps = value;
		}
	}

	public Action<TimeSpan>? Tick
	{
		get
		{
			return _tick;
		}
		set
		{
			if (value != null)
			{
				_tick = value;
				_stopped = false;
				if (!_threadStarted)
				{
					_threadStarted = true;
					Thread thread = new Thread(LoopProc);
					thread.IsBackground = true;
					thread.Start();
				}
				else
				{
					_wakeEvent.Set();
				}
			}
			else
			{
				_stopped = true;
				_tick = null;
			}
		}
	}

	public bool RunsInBackground => true;

	public SleepLoopRenderTimer(int fps)
	{
		DesiredFps = fps;
	}

	private void LoopProc()
	{
		TimeSpan timeSpan = _st.Elapsed;
		while (true)
		{
			if (_stopped)
			{
				_wakeEvent.WaitOne();
			}
			TimeSpan elapsed = _st.Elapsed;
			TimeSpan timeSpan2 = TimeSpan.FromSeconds(1.0 / (double)_desiredFps);
			TimeSpan timeout = timeSpan + timeSpan2 - elapsed;
			if (timeout.TotalMilliseconds > 1.0)
			{
				_wakeEvent.WaitOne(timeout);
			}
			timeSpan = (elapsed = _st.Elapsed);
			_tick?.Invoke(elapsed);
		}
	}
}
