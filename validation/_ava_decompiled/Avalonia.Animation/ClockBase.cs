using System;
using Avalonia.Reactive;

namespace Avalonia.Animation;

internal class ClockBase : IClock, IObservable<TimeSpan>
{
	private sealed class ClockObservable : LightweightObservableBase<TimeSpan>
	{
		public bool HasSubscriptions { get; private set; }

		public void Pulse(TimeSpan time)
		{
			PublishNext(time);
		}

		protected override void Initialize()
		{
			HasSubscriptions = true;
		}

		protected override void Deinitialize()
		{
			HasSubscriptions = false;
		}
	}

	private readonly ClockObservable _observable;

	private TimeSpan? _previousTime;

	private TimeSpan _internalTime;

	protected bool HasSubscriptions => _observable.HasSubscriptions;

	public PlayState PlayState { get; set; }

	protected ClockBase()
	{
		_observable = new ClockObservable();
	}

	protected void Pulse(TimeSpan systemTime)
	{
		if (!_previousTime.HasValue)
		{
			_previousTime = systemTime;
			_internalTime = TimeSpan.Zero;
		}
		else
		{
			if (PlayState == PlayState.Pause)
			{
				_previousTime = systemTime;
				return;
			}
			TimeSpan? previousTime = _previousTime;
			_internalTime += (systemTime - previousTime).Value;
			_previousTime = systemTime;
		}
		_observable.Pulse(_internalTime);
		if (PlayState == PlayState.Stop)
		{
			Stop();
		}
	}

	protected virtual void Stop()
	{
	}

	public IDisposable Subscribe(IObserver<TimeSpan> observer)
	{
		return _observable.Subscribe(observer);
	}
}
