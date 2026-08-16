using System;
using System.Runtime.ExceptionServices;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Animation;

/// <summary>
/// Handles the timing and lifetime of a <see cref="T:Avalonia.Animation.Transition`1" />.
/// </summary>
internal class TransitionInstance : SingleSubscriberObservableBase<double>, IObserver<TimeSpan>
{
	/// <summary>
	/// TODO: This clock is still fairly expensive due to <see cref="T:Avalonia.Animation.ClockBase" /> implementation.
	/// </summary>
	private sealed class TransitionClock : ClockBase, IObserver<TimeSpan>
	{
		private readonly IDisposable _parentSubscription;

		public TransitionClock(IClock parent)
		{
			_parentSubscription = parent.Subscribe(this);
		}

		protected override void Stop()
		{
			_parentSubscription.Dispose();
		}

		void IObserver<TimeSpan>.OnNext(TimeSpan value)
		{
			Pulse(value);
		}

		void IObserver<TimeSpan>.OnCompleted()
		{
		}

		void IObserver<TimeSpan>.OnError(Exception error)
		{
			ExceptionDispatchInfo.Capture(error).Throw();
		}
	}

	private IDisposable? _timerSubscription;

	private TimeSpan _delay;

	private TimeSpan _duration;

	private readonly IClock _baseClock;

	private TransitionClock? _clock;

	public TransitionInstance(IClock clock, TimeSpan delay, TimeSpan duration)
	{
		clock = clock ?? throw new ArgumentNullException("clock");
		_delay = delay;
		_duration = duration;
		_baseClock = clock;
	}

	private void TimerTick(TimeSpan t)
	{
		double num;
		if (t < _delay)
		{
			num = 0.0;
		}
		else if (MathUtilities.AreClose(_duration.TotalSeconds, 0.0))
		{
			num = 1.0;
		}
		else
		{
			TimeSpan timeSpan = _delay + _duration;
			double num2 = _delay.TotalSeconds / timeSpan.TotalSeconds;
			double num3 = t.TotalSeconds / timeSpan.TotalSeconds;
			num = ((!(num3 < num2) && !MathUtilities.AreClose(num3, num2)) ? ((t.TotalSeconds - _delay.TotalSeconds) / _duration.TotalSeconds) : 0.0);
		}
		if (num >= 1.0 || num < 0.0)
		{
			PublishNext(1.0);
			PublishCompleted();
		}
		else
		{
			PublishNext(num);
		}
	}

	protected override void Unsubscribed()
	{
		_timerSubscription?.Dispose();
		_clock.PlayState = PlayState.Stop;
	}

	protected override void Subscribed()
	{
		_clock = new TransitionClock(_baseClock);
		_timerSubscription = _clock.Subscribe(this);
		PublishNext(0.0);
	}

	void IObserver<TimeSpan>.OnCompleted()
	{
		PublishCompleted();
	}

	void IObserver<TimeSpan>.OnError(Exception error)
	{
		PublishError(error);
	}

	void IObserver<TimeSpan>.OnNext(TimeSpan value)
	{
		TimerTick(value);
	}
}
