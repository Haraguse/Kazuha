using System;
using Avalonia.Reactive;

namespace Avalonia.Threading;

/// <summary>
///     A timer that is integrated into the Dispatcher queues, and will
///     be processed after a given amount of time at a specified priority.
/// </summary>
public class DispatcherTimer
{
	private readonly object _instanceLock = new object();

	private readonly Dispatcher _dispatcher;

	private readonly DispatcherPriority _priority;

	private TimeSpan _interval;

	private DispatcherOperation? _operation;

	private bool _isEnabled;

	internal static int ActiveTimersCount { get; private set; }

	/// <summary>
	///     Gets the dispatcher this timer is associated with.
	/// </summary>
	public Dispatcher Dispatcher => _dispatcher;

	/// <summary>
	///     Gets or sets whether the timer is running.
	/// </summary>
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			lock (_instanceLock)
			{
				if (!value && _isEnabled)
				{
					Stop();
				}
				else if (value && !_isEnabled)
				{
					Start();
				}
			}
		}
	}

	/// <summary>
	///     Gets or sets the time between timer ticks.
	/// </summary>
	public TimeSpan Interval
	{
		get
		{
			return _interval;
		}
		set
		{
			bool flag = false;
			if (value.TotalMilliseconds < 0.0)
			{
				throw new ArgumentOutOfRangeException("value", "TimeSpan period must be greater than or equal to zero.");
			}
			lock (_instanceLock)
			{
				_interval = value;
				if (_isEnabled)
				{
					DueTimeInMs = _dispatcher.Now + (long)_interval.TotalMilliseconds;
					flag = true;
				}
			}
			if (flag)
			{
				_dispatcher.RescheduleTimers();
			}
		}
	}

	/// <summary>
	///     Any data that the caller wants to pass along with the timer.
	/// </summary>
	public object? Tag { get; set; }

	internal long DueTimeInMs { get; private set; }

	/// <summary>
	///     Occurs when the specified timer interval has elapsed and the
	///     timer is enabled.
	/// </summary>
	public event EventHandler? Tick;

	/// <summary>
	/// Creates a timer that uses <see cref="P:Avalonia.Threading.Dispatcher.CurrentDispatcher" /> to
	/// process the timer event at background priority.
	/// </summary>
	public DispatcherTimer()
		: this(TimeSpan.Zero, DispatcherPriority.Background, Avalonia.Threading.Dispatcher.CurrentDispatcher)
	{
	}

	/// <summary>
	/// Creates a timer that uses <see cref="P:Avalonia.Threading.Dispatcher.CurrentDispatcher" /> to
	/// process the timer event at the specified priority.
	/// </summary>
	/// <param name="priority">The priority to process the timer at.</param>
	public DispatcherTimer(DispatcherPriority priority)
		: this(TimeSpan.Zero, priority, Avalonia.Threading.Dispatcher.CurrentDispatcher)
	{
	}

	/// <summary>
	/// Creates a timer that uses the specified <see cref="T:Avalonia.Threading.Dispatcher" /> to
	/// process the timer event at the specified priority.
	/// </summary>
	/// <param name="priority">The priority to process the timer at.</param>
	/// <param name="dispatcher">The dispatcher to use to process the timer.</param>
	public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher)
		: this(TimeSpan.Zero, priority, dispatcher)
	{
	}

	/// <summary>
	/// Creates a timer that uses the specified <see cref="T:Avalonia.Threading.Dispatcher" /> to
	/// process the timer event at the specified priority after the specified timeout.
	/// </summary>
	/// <param name="interval">The interval to tick the timer after.</param>
	/// <param name="priority">The priority to process the timer at.</param>
	/// <param name="dispatcher">The dispatcher to use to process the timer.</param>
	public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, Dispatcher dispatcher)
	{
		ArgumentNullException.ThrowIfNull(dispatcher, "dispatcher");
		DispatcherPriority.Validate(priority, "priority");
		if (priority == DispatcherPriority.Inactive)
		{
			throw new ArgumentException("Specified priority is not valid.", "priority");
		}
		double totalMilliseconds = interval.TotalMilliseconds;
		if (totalMilliseconds < 0.0)
		{
			throw new ArgumentOutOfRangeException("interval", "TimeSpan period must be greater than or equal to zero.");
		}
		if (totalMilliseconds > 2147483647.0)
		{
			throw new ArgumentOutOfRangeException("interval", "TimeSpan period must be less than or equal to Int32.MaxValue.");
		}
		_dispatcher = dispatcher;
		_priority = priority;
		_interval = interval;
	}

	/// <summary>
	/// Creates a timer that uses <see cref="P:Avalonia.Threading.Dispatcher.CurrentDispatcher" /> to
	/// process the timer event at the specified priority after the specified timeout and with
	/// the specified handler.
	/// </summary>
	/// <param name="interval">The interval to tick the timer after.</param>
	/// <param name="priority">The priority to process the timer at.</param>
	/// <param name="callback">The callback to call when the timer ticks.</param>
	/// <remarks>This constructor immediately starts the timer.</remarks>
	public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback)
		: this(interval, priority, Avalonia.Threading.Dispatcher.CurrentDispatcher, callback)
	{
	}

	/// <summary>
	/// Creates a timer that uses the specified <see cref="T:Avalonia.Threading.Dispatcher" /> to
	/// process the timer event at the specified priority after the specified timeout and with
	/// the specified handler.
	/// </summary>
	/// <param name="interval">The interval to tick the timer after.</param>
	/// <param name="priority">The priority to process the timer at.</param>
	/// <param name="dispatcher">The dispatcher to use to process the timer.</param>
	/// <param name="callback">The callback to call when the timer ticks.</param>
	/// <remarks>This constructor immediately starts the timer.</remarks>
	public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, Dispatcher dispatcher, EventHandler callback)
		: this(interval, priority, dispatcher)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		Tick += callback;
		Start();
	}

	/// <summary>
	///     Starts the timer.
	/// </summary>
	public void Start()
	{
		lock (_instanceLock)
		{
			if (!_isEnabled)
			{
				_isEnabled = true;
				ActiveTimersCount++;
				Restart();
			}
		}
	}

	/// <summary>
	///     Stops the timer.
	/// </summary>
	public void Stop()
	{
		bool flag = false;
		lock (_instanceLock)
		{
			if (_isEnabled)
			{
				_isEnabled = false;
				ActiveTimersCount--;
				flag = true;
				if (_operation != null)
				{
					_operation.Abort();
					_operation = null;
				}
			}
		}
		if (flag)
		{
			_dispatcher.RemoveTimer(this);
		}
	}

	/// <summary>
	/// Starts a new timer.
	/// </summary>
	/// <param name="action">
	/// The method to call on timer tick. If the method returns false, the timer will stop.
	/// </param>
	/// <param name="interval">The interval at which to tick.</param>
	/// <param name="priority">The priority to use.</param>
	/// <returns>An <see cref="T:System.IDisposable" /> used to cancel the timer.</returns>
	public static IDisposable Run(Func<bool> action, TimeSpan interval, DispatcherPriority priority = default(DispatcherPriority))
	{
		DispatcherTimer timer = new DispatcherTimer(priority)
		{
			Interval = interval
		};
		timer.Tick += delegate
		{
			if (!action())
			{
				timer.Stop();
			}
		};
		timer.Start();
		return Disposable.Create(delegate
		{
			timer.Stop();
		});
	}

	/// <summary>
	/// Runs a method once, after the specified interval.
	/// </summary>
	/// <param name="action">
	/// The method to call after the interval has elapsed.
	/// </param>
	/// <param name="interval">The interval after which to call the method.</param>
	/// <param name="priority">The priority to use.</param>
	/// <returns>An <see cref="T:System.IDisposable" /> used to cancel the timer.</returns>
	public static IDisposable RunOnce(Action action, TimeSpan interval, DispatcherPriority priority = default(DispatcherPriority))
	{
		interval = ((interval != TimeSpan.Zero) ? interval : TimeSpan.FromTicks(1L));
		DispatcherTimer timer = new DispatcherTimer(priority)
		{
			Interval = interval
		};
		timer.Tick += delegate
		{
			action();
			timer.Stop();
		};
		timer.Start();
		return Disposable.Create(delegate
		{
			timer.Stop();
		});
	}

	private void Restart()
	{
		lock (_instanceLock)
		{
			if (_operation == null)
			{
				_operation = _dispatcher.InvokeAsync((Action)FireTick, DispatcherPriority.Inactive);
				DueTimeInMs = _dispatcher.Now + (long)_interval.TotalMilliseconds;
				if (_interval.TotalMilliseconds == 0.0 && _dispatcher.CheckAccess())
				{
					Promote();
				}
				else
				{
					_dispatcher.AddTimer(this);
				}
			}
		}
	}

	internal void Promote()
	{
		lock (_instanceLock)
		{
			if (_operation != null)
			{
				_operation.Priority = _priority;
			}
		}
	}

	private void FireTick()
	{
		_operation = null;
		if (Tick != null)
		{
			Tick(this, EventArgs.Empty);
		}
		if (_isEnabled)
		{
			Restart();
		}
	}
}
