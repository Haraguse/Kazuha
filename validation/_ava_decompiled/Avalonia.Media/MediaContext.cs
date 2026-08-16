using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media;

internal class MediaContext : ICompositorScheduler
{
	private class MediaContextClock : IGlobalClock, IClock, IObservable<TimeSpan>
	{
		private readonly MediaContext _parent;

		private readonly List<IObserver<TimeSpan>> _observers = new List<IObserver<TimeSpan>>();

		private readonly List<IObserver<TimeSpan>> _newObservers = new List<IObserver<TimeSpan>>();

		private Queue<Action<TimeSpan>> _queuedAnimationFrames = new Queue<Action<TimeSpan>>();

		private Queue<Action<TimeSpan>> _queuedAnimationFramesNext = new Queue<Action<TimeSpan>>();

		private TimeSpan _currentAnimationTimestamp;

		public bool HasNewSubscriptions => _newObservers.Count > 0;

		public bool HasSubscriptions
		{
			get
			{
				if (_observers.Count <= 0)
				{
					return _queuedAnimationFrames.Count > 0;
				}
				return true;
			}
		}

		public PlayState PlayState
		{
			get
			{
				return PlayState.Run;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public MediaContextClock(MediaContext parent)
		{
			_parent = parent;
		}

		public IDisposable Subscribe(IObserver<TimeSpan> observer)
		{
			_parent.ScheduleRender(now: false);
			_parent._dispatcher.VerifyAccess();
			_observers.Add(observer);
			_newObservers.Add(observer);
			return Disposable.Create(delegate
			{
				_parent._dispatcher.VerifyAccess();
				_observers.Remove(observer);
			});
		}

		public void RequestAnimationFrame(Action<TimeSpan> action)
		{
			_parent.ScheduleRender(now: false);
			_queuedAnimationFrames.Enqueue(action);
		}

		public void Pulse(TimeSpan now)
		{
			_newObservers.Clear();
			_currentAnimationTimestamp = now;
			Queue<Action<TimeSpan>> queuedAnimationFramesNext = _queuedAnimationFramesNext;
			Queue<Action<TimeSpan>> queuedAnimationFrames = _queuedAnimationFrames;
			_queuedAnimationFrames = queuedAnimationFramesNext;
			_queuedAnimationFramesNext = queuedAnimationFrames;
			Queue<Action<TimeSpan>> queuedAnimationFramesNext2 = _queuedAnimationFramesNext;
			Action<TimeSpan> result;
			while (queuedAnimationFramesNext2.TryDequeue(out result))
			{
				result(now);
			}
			IObserver<TimeSpan>[] array = _observers.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnNext(_currentAnimationTimestamp);
			}
		}

		public void PulseNewSubscriptions()
		{
			IObserver<TimeSpan>[] array = _newObservers.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnNext(_currentAnimationTimestamp);
			}
			_newObservers.Clear();
		}
	}

	private record TopLevelInfo(Compositor Compositor, CompositingRenderer Renderer, ILayoutManager LayoutManager);

	private readonly MediaContextClock _clock;

	private readonly Stopwatch _time = Stopwatch.StartNew();

	private DispatcherOperation? _nextRenderOp;

	private DispatcherOperation? _inputMarkerOp;

	private TimeSpan _inputMarkerAddedAt;

	private bool _isRendering;

	private bool _animationsAreWaitingForComposition;

	private readonly double MaxSecondsWithoutInput;

	private readonly Action _render;

	private readonly Action _inputMarkerHandler;

	private readonly HashSet<Compositor> _requestedCommits = new HashSet<Compositor>();

	private readonly Dictionary<Compositor, CompositionBatch> _pendingCompositionBatches = new Dictionary<Compositor, CompositionBatch>();

	private readonly Dispatcher _dispatcher;

	private List<Action>? _invokeOnRenderCallbacks;

	private readonly Stack<List<Action>> _invokeOnRenderCallbackListPool = new Stack<List<Action>>();

	private readonly DispatcherTimer _animationsTimer = new DispatcherTimer(DispatcherPriority.Render)
	{
		Interval = TimeSpan.FromMilliseconds(16L)
	};

	private readonly Dictionary<object, TopLevelInfo> _topLevels = new Dictionary<object, TopLevelInfo>();

	public IGlobalClock Clock => _clock;

	public static MediaContext Instance
	{
		get
		{
			MediaContext mediaContext = AvaloniaLocator.Current.GetService<MediaContext>();
			if (mediaContext == null)
			{
				DispatcherOptions dispatcherOptions = AvaloniaLocator.Current.GetService<DispatcherOptions>() ?? new DispatcherOptions();
				mediaContext = new MediaContext(Dispatcher.UIThread, dispatcherOptions.InputStarvationTimeout);
				AvaloniaLocator.CurrentMutable.Bind<MediaContext>().ToConstant(mediaContext);
			}
			return mediaContext;
		}
	}

	public void RequestAnimationFrame(Action<TimeSpan> action)
	{
		_clock.RequestAnimationFrame(action);
	}

	/// <summary>
	/// Actually sends the current batch to the compositor and does the required housekeeping
	/// This is the only place that should be allowed to call Commit
	/// </summary>
	private CompositionBatch CommitCompositor(Compositor compositor)
	{
		_requestedCommits.Remove(compositor);
		CompositionBatch commit = compositor.Commit();
		_pendingCompositionBatches[compositor] = commit;
		commit.Processed.ContinueWith(delegate
		{
			_dispatcher.Post(delegate
			{
				CompositionBatchFinished(compositor, commit);
			}, DispatcherPriority.Send);
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		return commit;
	}

	/// <summary>
	/// Handles batch completion, required to re-schedule a render pass if one was skipped due to compositor throttling
	/// </summary>
	private void CompositionBatchFinished(Compositor compositor, CompositionBatch batch)
	{
		if (_pendingCompositionBatches.TryGetValue(compositor, out CompositionBatch value) && value == batch)
		{
			_pendingCompositionBatches.Remove(compositor);
		}
		if (_pendingCompositionBatches.Count == 0)
		{
			_animationsAreWaitingForComposition = false;
			if (_requestedCommits.Count != 0 || _clock.HasSubscriptions)
			{
				ScheduleRender(now: false);
			}
		}
	}

	/// <summary>
	/// Triggers a composition commit if any batches are waiting to be sent,
	/// handles throttling
	/// </summary>
	/// <returns>true if there are pending commits in-flight and there will be a "all-done" callback later</returns>
	private bool CommitCompositorsWithThrottling()
	{
		Dispatcher.UIThread.VerifyAccess();
		if (_pendingCompositionBatches.Count > 0)
		{
			return true;
		}
		if (_requestedCommits.Count == 0)
		{
			return false;
		}
		Compositor[] array = _requestedCommits.ToArray();
		foreach (Compositor compositor in array)
		{
			CommitCompositor(compositor);
		}
		return true;
	}

	/// <summary>
	/// Executes a synchronous commit when we need to wait for composition jobs to be done
	/// Is used in resize and TopLevel destruction scenarios
	/// </summary>
	private void SyncCommit(Compositor compositor, bool waitFullRender, bool catchExceptions)
	{
		if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() == null)
		{
			return;
		}
		using (NonPumpingLockHelper.Use())
		{
			SyncWaitCompositorBatch(compositor, CommitCompositor(compositor), waitFullRender, catchExceptions);
		}
	}

	private void SyncWaitCompositorBatch(Compositor compositor, CompositionBatch batch, bool waitFullRender, bool catchExceptions)
	{
		using (NonPumpingLockHelper.Use())
		{
			if (compositor != null && !compositor.UseUiThreadForSynchronousCommits)
			{
				IRenderLoop loop = compositor.Loop;
				if (loop != null && loop.RunsInBackground)
				{
					(waitFullRender ? batch.Rendered : batch.Processed).Wait();
					return;
				}
			}
			compositor.Server.Render(catchExceptions);
		}
	}

	/// <summary>
	/// This method handles synchronous rendering of a surface when requested by the OS (typically during the resize)
	/// </summary>
	public void ImmediateRenderRequested(CompositionTarget target, bool catchExceptions)
	{
		SyncCommit(target.Compositor, waitFullRender: true, catchExceptions);
	}

	/// <summary>
	/// This method handles synchronous destruction of the composition target, so we are guaranteed
	/// to release all resources when a TopLevel is being destroyed 
	/// </summary>
	public void SyncDisposeCompositionTarget(CompositionTarget compositionTarget)
	{
		using (NonPumpingLockHelper.Use())
		{
			CompositionBatch batch = compositionTarget.Compositor.OobDispose(compositionTarget);
			SyncWaitCompositorBatch(compositionTarget.Compositor, batch, waitFullRender: false, catchExceptions: true);
		}
	}

	/// <summary>
	/// This method schedules a render when something has called RequestCommitAsync
	/// This can be triggered by user code outside of our normal layout and rendering
	/// </summary>
	void ICompositorScheduler.CommitRequested(Compositor compositor)
	{
		if (_requestedCommits.Add(compositor))
		{
			ScheduleRender(now: false);
		}
	}

	private MediaContext(Dispatcher dispatcher, TimeSpan inputStarvationTimeout)
	{
		_render = Render;
		_inputMarkerHandler = InputMarkerHandler;
		_clock = new MediaContextClock(this);
		_dispatcher = dispatcher;
		MaxSecondsWithoutInput = inputStarvationTimeout.TotalSeconds;
		_animationsTimer.Tick += delegate
		{
			_animationsTimer.Stop();
			ScheduleRender(now: false);
		};
	}

	/// <summary>
	/// Schedules the next render operation, handles render throttling for input processing
	/// </summary>
	private void ScheduleRender(bool now)
	{
		if (_nextRenderOp != null)
		{
			if (now)
			{
				_nextRenderOp.Priority = DispatcherPriority.Render;
			}
			return;
		}
		DispatcherPriority priority = DispatcherPriority.Render;
		if (_inputMarkerOp == null)
		{
			_inputMarkerOp = new DispatcherOperation(_dispatcher, DispatcherPriority.Input, _inputMarkerHandler, throwOnUiThread: true, captureExecutionContext: false);
			_dispatcher.InvokeAsyncImpl(_inputMarkerOp, CancellationToken.None);
			_inputMarkerAddedAt = _time.Elapsed;
		}
		else if (!now && (_time.Elapsed - _inputMarkerAddedAt).TotalSeconds > MaxSecondsWithoutInput)
		{
			priority = DispatcherPriority.Input;
		}
		DispatcherOperation operation = (_nextRenderOp = new DispatcherOperation(_dispatcher, priority, _render, throwOnUiThread: true, captureExecutionContext: false));
		_dispatcher.InvokeAsyncImpl(operation, CancellationToken.None);
	}

	/// <summary>
	/// This handles the _inputMarkerOp message.  We're using
	/// _inputMarkerOp to determine if input priority dispatcher ops
	/// have been processes.
	/// </summary>
	private void InputMarkerHandler()
	{
		_inputMarkerOp = null;
	}

	private void Render()
	{
		try
		{
			_isRendering = true;
			RenderCore();
		}
		finally
		{
			_nextRenderOp = null;
			_isRendering = false;
		}
	}

	private void RenderCore()
	{
		TimeSpan elapsed = _time.Elapsed;
		if (!_animationsAreWaitingForComposition)
		{
			_clock.Pulse(elapsed);
		}
		for (int i = 0; i < 10; i++)
		{
			FireInvokeOnRenderCallbacks();
			if (!_clock.HasNewSubscriptions)
			{
				break;
			}
			_clock.PulseNewSubscriptions();
		}
		if (_requestedCommits.Count > 0 || _clock.HasSubscriptions)
		{
			_animationsAreWaitingForComposition = CommitCompositorsWithThrottling();
			if (!_animationsAreWaitingForComposition && _clock.HasSubscriptions)
			{
				_animationsTimer.Start();
			}
		}
	}

	public bool IsTopLevelActive(object key)
	{
		return _topLevels.ContainsKey(key);
	}

	public void AddTopLevel(object key, ILayoutManager layoutManager, IRenderer renderer)
	{
		if (!_topLevels.ContainsKey(key))
		{
			CompositingRenderer compositingRenderer = (CompositingRenderer)renderer;
			_topLevels.Add(key, new TopLevelInfo(compositingRenderer.Compositor, compositingRenderer, layoutManager));
			compositingRenderer.Start();
			ScheduleRender(now: true);
		}
	}

	public void RemoveTopLevel(object key)
	{
		if (_topLevels.Remove(key, out TopLevelInfo value))
		{
			value.Renderer.Stop();
		}
	}

	/// <summary>
	/// Calls all _invokeOnRenderCallbacks until no more are added
	/// </summary>
	private void FireInvokeOnRenderCallbacks()
	{
		int num = 0;
		int num2 = _invokeOnRenderCallbacks?.Count ?? 0;
		while (true)
		{
			if (num2 > 0)
			{
				num++;
				if (num > 153)
				{
					throw new InvalidOperationException("Infinite layout loop detected");
				}
				List<Action> invokeOnRenderCallbacks = _invokeOnRenderCallbacks;
				_invokeOnRenderCallbacks = null;
				for (int i = 0; i < num2; i++)
				{
					invokeOnRenderCallbacks[i]();
				}
				invokeOnRenderCallbacks.Clear();
				_invokeOnRenderCallbackListPool.Push(invokeOnRenderCallbacks);
				num2 = _invokeOnRenderCallbacks?.Count ?? 0;
			}
			else
			{
				num2 = _invokeOnRenderCallbacks?.Count ?? 0;
				if (num2 <= 0)
				{
					break;
				}
			}
		}
	}

	/// <summary>
	/// Executes the <paramref name="callback">callback</paramref> in the next iteration of the current UI-thread
	/// render loop / layout pass that.
	/// </summary>
	/// <param name="callback"></param>
	public void BeginInvokeOnRender(Action callback)
	{
		if (_invokeOnRenderCallbacks == null)
		{
			_invokeOnRenderCallbacks = ((_invokeOnRenderCallbackListPool.Count > 0) ? _invokeOnRenderCallbackListPool.Pop() : new List<Action>());
		}
		_invokeOnRenderCallbacks.Add(callback);
		if (!_isRendering)
		{
			ScheduleRender(now: true);
		}
	}
}
