using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// A pool that keeps a number of elements that was used in the last 10 seconds 
/// </summary>
internal abstract class BatchStreamPoolBase<T> : IDisposable
{
	private readonly Action<Func<bool>>? _startTimer;

	private readonly Stack<T> _pool = new Stack<T>();

	private bool _disposed;

	private int _usage;

	private readonly int[] _usageStatistics = new int[10];

	private int _usageStatisticsSlot;

	private readonly WeakReference<BatchStreamPoolBase<T>> _updateRef;

	private readonly Dispatcher? _reclaimOnDispatcher;

	private bool _timerIsRunning;

	private ulong _currentUpdateTick;

	private ulong _lastActivityTick;

	public int CurrentUsage => _usage;

	public int CurrentPool => _pool.Count;

	[MemberNotNullWhen(true, "_reclaimOnDispatcher")]
	private bool NeedsTimer
	{
		[MemberNotNullWhen(true, "_reclaimOnDispatcher")]
		get
		{
			if (_reclaimOnDispatcher != null)
			{
				return _currentUpdateTick - _lastActivityTick < (uint)(_usageStatistics.Length * 2 + 1);
			}
			return false;
		}
	}

	private bool ReclaimImmediately => _reclaimOnDispatcher == null;

	public BatchStreamPoolBase(bool needsFinalize, bool reclaimImmediately, Action<Func<bool>>? startTimer = null)
	{
		_startTimer = startTimer;
		if (!needsFinalize)
		{
			GC.SuppressFinalize(this);
		}
		_updateRef = new WeakReference<BatchStreamPoolBase<T>>(this);
		_reclaimOnDispatcher = ((!reclaimImmediately) ? Dispatcher.FromThread(Thread.CurrentThread) : null);
		EnsureUpdateTimer();
	}

	private void EnsureUpdateTimer()
	{
		if (_timerIsRunning || !NeedsTimer)
		{
			return;
		}
		Func<bool> timerProc = GetTimerProc(_updateRef);
		if (_startTimer != null)
		{
			_startTimer(timerProc);
		}
		else if (_reclaimOnDispatcher != null)
		{
			if (_reclaimOnDispatcher.CheckAccess())
			{
				DispatcherTimer.Run(timerProc, TimeSpan.FromSeconds(1L));
			}
			else
			{
				_reclaimOnDispatcher.Post(delegate
				{
					DispatcherTimer.Run(timerProc, TimeSpan.FromSeconds(1L));
				}, DispatcherPriority.Normal);
			}
		}
		_timerIsRunning = true;
		static Func<bool> GetTimerProc(WeakReference<BatchStreamPoolBase<T>> updateRef)
		{
			return () => updateRef.TryGetTarget(out BatchStreamPoolBase<T> target) && target.UpdateTimerTick();
		}
	}

	private bool UpdateTimerTick()
	{
		lock (_pool)
		{
			_currentUpdateTick++;
			int num = Math.Max(_usageStatistics.Max() - _usage, 10);
			while (num < _pool.Count)
			{
				DestroyItem(_pool.Pop());
			}
			_usageStatisticsSlot = (_usageStatisticsSlot + 1) % _usageStatistics.Length;
			_usageStatistics[_usageStatisticsSlot] = 0;
			return _timerIsRunning = NeedsTimer;
		}
	}

	private void OnActivity()
	{
		_lastActivityTick = _currentUpdateTick;
		EnsureUpdateTimer();
	}

	protected abstract T CreateItem();

	protected virtual void ClearItem(T item)
	{
	}

	protected virtual void DestroyItem(T item)
	{
	}

	public T Get()
	{
		lock (_pool)
		{
			_usage++;
			if (_usageStatistics[_usageStatisticsSlot] < _usage)
			{
				_usageStatistics[_usageStatisticsSlot] = _usage;
			}
			OnActivity();
			if (_pool.Count != 0)
			{
				return _pool.Pop();
			}
		}
		return CreateItem();
	}

	public void Return(T item)
	{
		ClearItem(item);
		lock (_pool)
		{
			_usage--;
			if (!_disposed && !ReclaimImmediately)
			{
				_pool.Push(item);
				OnActivity();
				return;
			}
		}
		DestroyItem(item);
	}

	public void Dispose()
	{
		lock (_pool)
		{
			_disposed = true;
			foreach (T item in _pool)
			{
				DestroyItem(item);
			}
			_pool.Clear();
		}
	}

	~BatchStreamPoolBase()
	{
		Dispose();
	}
}
