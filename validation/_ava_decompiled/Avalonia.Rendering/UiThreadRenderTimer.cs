using System;
using System.Diagnostics;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Rendering;

/// <summary>
/// Render timer that ticks on UI thread. Useful for debugging or bootstrapping on new platforms 
/// </summary>
[PrivateApi]
public class UiThreadRenderTimer : DefaultRenderTimer
{
	private class TimerInstance : IDisposable
	{
		private UiThreadRenderTimer _parent;

		private readonly Action<TimeSpan> _tick;

		private DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Render);

		private static readonly TimeSpan s_minInterval = TimeSpan.FromMilliseconds(1L);

		private TimeSpan Interval { get; }

		public TimerInstance(UiThreadRenderTimer parent, Action<TimeSpan> tick)
		{
			_parent = parent;
			_tick = tick;
			_timer.Tick += OnTick;
			_timer.Interval = Interval;
			Interval = TimeSpan.FromSeconds(1.0 / (double)_parent.FramesPerSecond);
			_timer.Start();
		}

		private void OnTick(object? sender, EventArgs e)
		{
			TimeSpan elapsed = _parent._clock.Elapsed;
			TimeSpan timeSpan = elapsed + Interval;
			try
			{
				_tick(elapsed);
			}
			finally
			{
				TimeSpan elapsed2 = _parent._clock.Elapsed;
				TimeSpan timeSpan2 = timeSpan - elapsed2;
				if (timeSpan2 < s_minInterval)
				{
					timeSpan2 = s_minInterval;
				}
				_timer.Interval = timeSpan2;
			}
		}

		public void Dispose()
		{
			_timer.Stop();
		}
	}

	private readonly Stopwatch _clock = Stopwatch.StartNew();

	/// <inheritdoc />
	public override bool RunsInBackground => false;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rendering.UiThreadRenderTimer" /> class.
	/// </summary>
	/// <param name="framesPerSecond">The number of frames per second at which the loop should run.</param>
	public UiThreadRenderTimer(int framesPerSecond)
		: base(framesPerSecond)
	{
	}

	/// <inheritdoc />
	protected override IDisposable StartCore(Action<TimeSpan> tick)
	{
		return new TimerInstance(this, tick);
	}
}
