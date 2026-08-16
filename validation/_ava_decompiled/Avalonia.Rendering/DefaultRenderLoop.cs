using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Rendering;

/// <summary>
/// Default implementation of the application render loop.
/// </summary>
/// <remarks>
/// The render loop is responsible for advancing the animation timer and updating the scene
/// graph for visible windows. It owns the sleep/wake state machine: setting
/// <see cref="P:Avalonia.Rendering.IRenderTimer.Tick" /> to a non-null callback to start the timer and to null to
/// stop it, under a lock so that timer implementations never see concurrent changes.
/// </remarks>
internal class DefaultRenderLoop : IRenderLoop
{
	private readonly List<IRenderLoopTask> _items = new List<IRenderLoopTask>();

	private readonly List<IRenderLoopTask> _itemsCopy = new List<IRenderLoopTask>();

	private Action<TimeSpan> _tick;

	private readonly IRenderTimer _timer;

	private readonly object _timerLock = new object();

	private int _inTick;

	private volatile bool _hasItems;

	private bool _running;

	private bool _wakeupPending;

	/// <inheritdoc />
	public bool RunsInBackground => _timer.RunsInBackground;

	internal IRenderTimer Timer => _timer;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Rendering.DefaultRenderLoop" /> class.
	/// </summary>
	/// <param name="timer">The render timer.</param>
	public DefaultRenderLoop(IRenderTimer timer)
	{
		_timer = timer;
		_tick = TimerTick;
	}

	/// <inheritdoc />
	public void Add(IRenderLoopTask i)
	{
		if (i == null)
		{
			throw new ArgumentNullException("i");
		}
		Dispatcher.UIThread.VerifyAccess();
		bool flag;
		lock (_items)
		{
			_items.Add(i);
			flag = _items.Count == 1;
		}
		if (flag)
		{
			_hasItems = true;
			Wakeup();
		}
	}

	/// <inheritdoc />
	public void Remove(IRenderLoopTask i)
	{
		if (i == null)
		{
			throw new ArgumentNullException("i");
		}
		Dispatcher.UIThread.VerifyAccess();
		bool flag;
		lock (_items)
		{
			_items.Remove(i);
			flag = _items.Count == 0;
		}
		if (!flag)
		{
			return;
		}
		_hasItems = false;
		lock (_timerLock)
		{
			if (_running)
			{
				_running = false;
				_wakeupPending = false;
				_timer.Tick = null;
			}
		}
	}

	/// <inheritdoc />
	public void Wakeup()
	{
		lock (_timerLock)
		{
			if (_hasItems && !_running)
			{
				_running = true;
				_timer.Tick = _tick;
			}
			else
			{
				_wakeupPending = true;
			}
		}
	}

	private void TimerTick(TimeSpan time)
	{
		if (Interlocked.CompareExchange(ref _inTick, 1, 0) != 0)
		{
			return;
		}
		try
		{
			lock (_timerLock)
			{
				if (!_running)
				{
					return;
				}
				_wakeupPending = false;
			}
			lock (_items)
			{
				_itemsCopy.Clear();
				_itemsCopy.AddRange(_items);
			}
			bool flag = false;
			for (int i = 0; i < _itemsCopy.Count; i++)
			{
				flag |= _itemsCopy[i].Render();
			}
			_itemsCopy.Clear();
			if (flag)
			{
				return;
			}
			lock (_timerLock)
			{
				if (_running)
				{
					if (_wakeupPending)
					{
						_wakeupPending = false;
						return;
					}
					_running = false;
					_timer.Tick = null;
				}
			}
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Error, "Visual")?.Log(this, "Exception in render loop: {Error}", propertyValue);
		}
		finally
		{
			Interlocked.Exchange(ref _inTick, 0);
		}
	}
}
