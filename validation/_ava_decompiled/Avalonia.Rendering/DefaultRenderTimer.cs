using System;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering;

/// <summary>
/// Defines a default render timer that uses a standard timer.
/// </summary>
/// <remarks>
/// This class may be overridden by platform implementations to use a specialized timer
/// implementation.
/// </remarks>
[PrivateApi]
public class DefaultRenderTimer : IRenderTimer
{
	private volatile Action<TimeSpan>? _tick;

	private IDisposable? _subscription;

	/// <summary>
	/// Gets the number of frames per second at which the loop runs.
	/// </summary>
	public int FramesPerSecond { get; }

	/// <inheritdoc />
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
				if (_subscription == null)
				{
					_subscription = StartCore(InternalTick);
				}
			}
			else
			{
				_subscription?.Dispose();
				_subscription = null;
				_tick = null;
			}
		}
	}

	/// <inheritdoc />
	public virtual bool RunsInBackground => true;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rendering.DefaultRenderTimer" /> class.
	/// </summary>
	/// <param name="framesPerSecond">
	/// The number of frames per second at which the loop should run.
	/// </param>
	public DefaultRenderTimer(int framesPerSecond)
	{
		FramesPerSecond = framesPerSecond;
	}

	/// <summary>
	/// Provides the implementation of starting the timer.
	/// </summary>
	/// <param name="tick">The method to call on each tick.</param>
	/// <remarks>
	/// This can be overridden by platform implementations to use a specialized timer
	/// implementation.
	/// </remarks>
	protected virtual IDisposable StartCore(Action<TimeSpan> tick)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(1.0 / (double)FramesPerSecond);
		return new Timer(delegate
		{
			tick(TimeSpan.FromMilliseconds(Environment.TickCount));
		}, null, timeSpan, timeSpan);
	}

	private void InternalTick(TimeSpan tickCount)
	{
		_tick?.Invoke(tickCount);
	}
}
