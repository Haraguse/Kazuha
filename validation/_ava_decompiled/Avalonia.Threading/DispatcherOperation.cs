using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

[DebuggerDisplay("{DebugDisplay}")]
public class DispatcherOperation
{
	private sealed class DispatcherOperationFrame : DispatcherFrame
	{
		private readonly DispatcherOperation _operation;

		private readonly Timer? _waitTimer;

		public DispatcherOperationFrame(DispatcherOperation op, TimeSpan timeout)
			: base(exitWhenRequested: false)
		{
			_operation = op;
			_operation.Aborted += OnCompletedOrAborted;
			_operation.Completed += OnCompletedOrAborted;
			if (timeout.TotalMilliseconds > 0.0)
			{
				_waitTimer = new Timer(delegate
				{
					Exit();
				}, null, timeout, TimeSpan.FromMilliseconds(-1L));
			}
			if (_operation.Status != DispatcherOperationStatus.Pending)
			{
				Exit();
			}
		}

		private void Exit()
		{
			base.Continue = false;
			if (_waitTimer != null)
			{
				_waitTimer.Dispose();
			}
			_operation.Aborted -= OnCompletedOrAborted;
			_operation.Completed -= OnCompletedOrAborted;
		}

		private void OnCompletedOrAborted(object? sender, EventArgs e)
		{
			Exit();
		}
	}

	protected readonly bool ThrowOnUiThread;

	protected internal object? Callback;

	protected object? TaskSource;

	private EventHandler? _aborted;

	private EventHandler? _completed;

	private DispatcherPriority _priority;

	private readonly CulturePreservingExecutionContext? _executionContext;

	private static readonly Task s_abortedTask = Task.FromCanceled(CreateCancelledToken());

	public DispatcherOperationStatus Status { get; internal set; }

	public Dispatcher Dispatcher { get; }

	public DispatcherPriority Priority
	{
		get
		{
			return _priority;
		}
		set
		{
			_priority = value;
			Dispatcher?.SetPriority(this, value);
		}
	}

	internal DispatcherOperation? SequentialPrev { get; set; }

	internal DispatcherOperation? SequentialNext { get; set; }

	internal DispatcherOperation? PriorityPrev { get; set; }

	internal DispatcherOperation? PriorityNext { get; set; }

	internal PriorityChain? Chain { get; set; }

	internal bool IsQueued => Chain != null;

	internal string DebugDisplay
	{
		get
		{
			MethodInfo methodInfo = (Callback as Delegate)?.Method;
			string value = (((object)methodInfo == null) ? "???" : (methodInfo.DeclaringType?.ToString() + "." + methodInfo.Name));
			return $"{value} [{Priority}]";
		}
	}

	/// <summary>
	///     An event that is raised when the operation is aborted or canceled.
	/// </summary>
	public event EventHandler Aborted
	{
		add
		{
			lock (Dispatcher.InstanceLock)
			{
				_aborted = (EventHandler)Delegate.Combine(_aborted, value);
			}
		}
		remove
		{
			lock (Dispatcher.InstanceLock)
			{
				_aborted = (EventHandler)Delegate.Remove(_aborted, value);
			}
		}
	}

	/// <summary>
	///     An event that is raised when the operation completes.
	/// </summary>
	/// <remarks>
	///     Completed indicates that the operation was invoked and has
	///     either completed successfully or faulted. Note that a canceled
	///     or aborted operation is never is never considered completed.
	/// </remarks>
	public event EventHandler Completed
	{
		add
		{
			lock (Dispatcher.InstanceLock)
			{
				_completed = (EventHandler)Delegate.Combine(_completed, value);
			}
		}
		remove
		{
			lock (Dispatcher.InstanceLock)
			{
				_completed = (EventHandler)Delegate.Remove(_completed, value);
			}
		}
	}

	internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Action callback, bool throwOnUiThread, bool captureExecutionContext = true)
		: this(dispatcher, priority, throwOnUiThread, captureExecutionContext)
	{
		Callback = callback;
	}

	private protected DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, bool throwOnUiThread, bool captureExecutionContext = true)
	{
		ThrowOnUiThread = throwOnUiThread;
		Priority = priority;
		Dispatcher = dispatcher;
		_executionContext = (captureExecutionContext ? CulturePreservingExecutionContext.Capture() : null);
	}

	public bool Abort()
	{
		if (Dispatcher.Abort(this))
		{
			CallAbortCallbacks();
			return true;
		}
		return false;
	}

	/// <summary>
	///     Waits for this operation to complete.
	/// </summary>
	/// <returns>
	///     The status of the operation.  To obtain the return value
	///     of the invoked delegate, use the Result property.
	/// </returns>
	public void Wait()
	{
		Wait(TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Waits for this operation to complete.
	/// </summary>
	/// <param name="timeout">
	///     The maximum amount of time to wait.
	/// </param>
	public void Wait(TimeSpan timeout)
	{
		if ((Status == DispatcherOperationStatus.Pending || Status == DispatcherOperationStatus.Executing) && timeout.TotalMilliseconds != 0.0 && Dispatcher.CheckAccess())
		{
			if (Status == DispatcherOperationStatus.Executing)
			{
				throw new InvalidOperationException("A thread cannot wait on operations already running on the same thread.");
			}
			CancellationTokenSource cts = new CancellationTokenSource();
			EventHandler value = delegate
			{
				cts.Cancel();
			};
			Completed += value;
			Aborted += value;
			try
			{
				while (Status == DispatcherOperationStatus.Pending)
				{
					if (Dispatcher.SupportsRunLoops)
					{
						if (Priority >= DispatcherPriority.MinimumForegroundPriority)
						{
							Dispatcher.RunJobs(Priority, cts.Token);
						}
						else
						{
							Dispatcher.PushFrame(new DispatcherOperationFrame(this, timeout));
						}
					}
					else
					{
						Dispatcher.RunJobs(DispatcherPriority.MinimumActiveValue, cts.Token);
					}
				}
			}
			finally
			{
				Completed -= value;
				Aborted -= value;
			}
		}
		GetTask().GetAwaiter().GetResult();
	}

	public Task GetTask()
	{
		return GetTaskCore();
	}

	/// <summary>
	///     Returns an awaiter for awaiting the completion of the operation.
	/// </summary>
	/// <remarks>
	///     This method is intended to be used by compilers.
	/// </remarks>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public TaskAwaiter GetAwaiter()
	{
		return GetTask().GetAwaiter();
	}

	internal void CallAbortCallbacks()
	{
		AbortTask();
		_aborted?.Invoke(this, EventArgs.Empty);
	}

	internal void Execute()
	{
		try
		{
			using (AvaloniaSynchronizationContext.Ensure(Dispatcher, Priority))
			{
				CulturePreservingExecutionContext executionContext = _executionContext;
				if (executionContext != null)
				{
					CulturePreservingExecutionContext.Run(executionContext, delegate(object? s)
					{
						((DispatcherOperation)s).InvokeCore();
					}, this);
				}
				else
				{
					InvokeCore();
				}
			}
		}
		finally
		{
			_completed?.Invoke(this, EventArgs.Empty);
		}
	}

	protected virtual void InvokeCore()
	{
		try
		{
			((Action)Callback)();
			lock (Dispatcher.InstanceLock)
			{
				Status = DispatcherOperationStatus.Completed;
				if (TaskSource is TaskCompletionSource<object> taskCompletionSource)
				{
					taskCompletionSource.SetResult(null);
				}
			}
		}
		catch (Exception ex)
		{
			lock (Dispatcher.InstanceLock)
			{
				GetTaskCore();
				Status = DispatcherOperationStatus.Completed;
				if (TaskSource is TaskCompletionSource<object> taskCompletionSource2)
				{
					taskCompletionSource2.SetException(ex);
				}
			}
			if (ThrowOnUiThread && !Dispatcher.TryCatchWhen(ex))
			{
				throw;
			}
		}
	}

	internal virtual object? GetResult()
	{
		return null;
	}

	protected virtual void AbortTask()
	{
		object taskSource;
		lock (Dispatcher.InstanceLock)
		{
			taskSource = TaskSource;
		}
		(taskSource as TaskCompletionSource<object>)?.SetCanceled();
	}

	private static CancellationToken CreateCancelledToken()
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		return cancellationTokenSource.Token;
	}

	protected virtual Task GetTaskCore()
	{
		lock (Dispatcher.InstanceLock)
		{
			if (Status == DispatcherOperationStatus.Aborted)
			{
				return s_abortedTask;
			}
			if (TaskSource is TaskCompletionSource<object> taskCompletionSource)
			{
				return taskCompletionSource.Task;
			}
			if (Status == DispatcherOperationStatus.Completed)
			{
				return Task.CompletedTask;
			}
			return ((TaskCompletionSource<object>)(TaskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously))).Task;
		}
	}
}
public class DispatcherOperation<T> : DispatcherOperation
{
	private TaskCompletionSource<T> TaskCompletionSource => (TaskCompletionSource<T>)TaskSource;

	public T Result
	{
		get
		{
			if (TaskCompletionSource.Task.IsCompleted || !base.Dispatcher.CheckAccess())
			{
				return TaskCompletionSource.Task.GetAwaiter().GetResult();
			}
			throw new InvalidOperationException("Synchronous wait is only supported on non-UI threads");
		}
	}

	public DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Func<T> callback)
		: base(dispatcher, priority, throwOnUiThread: false)
	{
		TaskSource = new TaskCompletionSource<T>();
		Callback = callback;
	}

	public new TaskAwaiter<T> GetAwaiter()
	{
		return GetTask().GetAwaiter();
	}

	public new Task<T> GetTask()
	{
		return TaskCompletionSource.Task;
	}

	protected override Task GetTaskCore()
	{
		return GetTask();
	}

	protected override void AbortTask()
	{
		TaskCompletionSource.SetCanceled();
	}

	internal override object? GetResult()
	{
		return GetTask().Result;
	}

	protected override void InvokeCore()
	{
		try
		{
			T result = ((Func<T>)Callback)();
			lock (base.Dispatcher.InstanceLock)
			{
				base.Status = DispatcherOperationStatus.Completed;
				TaskCompletionSource.SetResult(result);
			}
		}
		catch (Exception exception)
		{
			lock (base.Dispatcher.InstanceLock)
			{
				base.Status = DispatcherOperationStatus.Completed;
				TaskCompletionSource.SetException(exception);
			}
		}
	}
}
