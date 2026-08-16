using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Threading;

/// <summary>
/// Provides services for managing work items on a thread.
/// </summary>
public class Dispatcher : IDispatcher
{
	public record struct DispatcherProcessingDisabled : IDisposable
	{
		private readonly SynchronizationContext? _oldContext;

		private readonly bool _restoreContext;

		private Dispatcher? _dispatcher;

		internal DispatcherProcessingDisabled(Dispatcher dispatcher)
		{
			_oldContext = null;
			_restoreContext = false;
			_dispatcher = dispatcher;
		}

		internal DispatcherProcessingDisabled(Dispatcher dispatcher, SynchronizationContext? oldContext)
			: this(dispatcher)
		{
			_oldContext = oldContext;
			_restoreContext = true;
		}

		public void Dispose()
		{
			if (_dispatcher != null)
			{
				_dispatcher.DisabledProcessingCount--;
				_dispatcher = null;
				if (_restoreContext)
				{
					SynchronizationContext.SetSynchronizationContext(_oldContext);
				}
			}
		}
	}

	private sealed class DummyShuttingDownUnitTestDispatcherImpl : IDispatcherImpl
	{
		public bool CurrentThreadIsLoopThread => true;

		public long Now => 0L;

		public event Action? Signaled
		{
			add
			{
			}
			remove
			{
			}
		}

		public event Action? Timer
		{
			add
			{
			}
			remove
			{
			}
		}

		public void Signal()
		{
		}

		public void UpdateTimer(long? dueTimeInMs)
		{
		}
	}

	private class DispatcherReferenceStorage
	{
		public WeakReference<Dispatcher> Reference = new WeakReference<Dispatcher>(null);
	}

	private IDispatcherImpl _impl;

	private bool _initialized;

	private IControlledDispatcherImpl? _controlledImpl;

	private IDispatcherImplWithPendingInput? _pendingInputImpl;

	private IDispatcherImplWithExplicitBackgroundProcessing? _backgroundProcessingImpl;

	private readonly Thread _thread;

	private readonly AvaloniaSynchronizationContext?[] _priorityContexts = new AvaloniaSynchronizationContext[(int)DispatcherPriority.MaxValue - (int)DispatcherPriority.MinValue + 1];

	internal static readonly object ExceptionDataKey = new object();

	private DispatcherUnhandledExceptionFilterEventHandler? _unhandledExceptionFilter;

	private DispatcherUnhandledExceptionEventArgs _unhandledExceptionEventArgs;

	private DispatcherUnhandledExceptionFilterEventArgs _exceptionFilterEventArgs;

	private bool _hasShutdownFinished;

	private bool _startingShutdown;

	private readonly Stack<DispatcherFrame> _frames = new Stack<DispatcherFrame>();

	private readonly DispatcherPriorityQueue _queue = new DispatcherPriorityQueue();

	private bool _signaled;

	private bool _explicitBackgroundProcessingRequested;

	private const int MaximumInputStarvationTimeInFallbackMode = 50;

	private const int MaximumInputStarvationTimeInExplicitProcessingExplicitMode = 50;

	private int _maximumInputStarvationTime;

	[ThreadStatic]
	private static DispatcherReferenceStorage? s_currentThreadDispatcher;

	private static readonly object s_globalLock = new object();

	private static readonly ConditionalWeakTable<Thread, DispatcherReferenceStorage> s_dispatchers = new ConditionalWeakTable<Thread, DispatcherReferenceStorage>();

	private static Dispatcher? s_uiThread;

	private readonly List<DispatcherTimer> _timers = new List<DispatcherTimer>();

	private long _timersVersion;

	private bool _dueTimeFound;

	private long _dueTimeInMs;

	private long? _dueTimeForTimers;

	private long? _dueTimeForBackgroundProcessing;

	private long? _osTimerSetTo;

	private readonly Func<long> _timeProvider;

	internal object InstanceLock { get; } = new object();

	public bool SupportsRunLoops => _controlledImpl != null;

	internal bool IsSta { get; }

	public Thread Thread => _thread;

	[PrivateApi]
	public IDispatcherImpl PlatformImpl => _impl;

	internal bool ExitAllFramesRequested { get; private set; }

	internal bool HasShutdownStarted { get; private set; }

	internal int DisabledProcessingCount { get; set; }

	public static Dispatcher CurrentDispatcher
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			DispatcherReferenceStorage? dispatcherReferenceStorage = s_currentThreadDispatcher;
			if (dispatcherReferenceStorage != null && dispatcherReferenceStorage.Reference.TryGetTarget(out Dispatcher target))
			{
				return target;
			}
			return new Dispatcher(null);
		}
	}

	/// <summary>
	/// Gets the dispatcher for the UI thread.
	/// </summary>
	/// <remarks>
	/// Control and libraries author are encouraged to use <see cref="P:Avalonia.Threading.Dispatcher.CurrentDispatcher" /> and
	/// <see cref="P:Avalonia.AvaloniaObject.Dispatcher" /> instead.
	/// </remarks>
	public static Dispatcher UIThread
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return s_uiThread ?? GetUIThreadDispatcherSlow();
			static Dispatcher GetUIThreadDispatcherSlow()
			{
				lock (s_globalLock)
				{
					return s_uiThread ?? CurrentDispatcher;
				}
			}
		}
	}

	internal long Now => _timeProvider();

	/// <summary>
	/// Occurs when a thread exception is thrown and uncaught during execution of a delegate by way of <see cref="M:Avalonia.Threading.Dispatcher.Invoke(System.Action)" /> or <see cref="M:Avalonia.Threading.Dispatcher.InvokeAsync(System.Action)" />.
	/// </summary>
	/// <remarks>
	/// This event is raised when an exception that was thrown during execution of a delegate by way of <see cref="M:Avalonia.Threading.Dispatcher.Invoke(System.Action)" /> or <see cref="M:Avalonia.Threading.Dispatcher.InvokeAsync(System.Action)" /> is uncaught.
	/// A handler can mark the exception as handled, which will prevent the internal exception handler from being called.
	/// Event handlers for this event must be written with care to avoid creating secondary exceptions and to catch any that occur. It is recommended to avoid allocating memory or doing any resource intensive operations in the handler.
	/// </remarks>
	public event DispatcherUnhandledExceptionEventHandler? UnhandledException;

	/// <summary>
	/// Occurs when a thread exception is thrown and uncaught during execution of a delegate by way of <see cref="M:Avalonia.Threading.Dispatcher.Invoke(System.Action)" /> or <see cref="M:Avalonia.Threading.Dispatcher.InvokeAsync(System.Action)" /> when in the filter stage.
	/// </summary>
	/// <remarks>
	/// This event is raised during the filter stage for an exception that is raised during execution of a delegate by way of <see cref="M:Avalonia.Threading.Dispatcher.Invoke(System.Action)" /> or <see cref="M:Avalonia.Threading.Dispatcher.InvokeAsync(System.Action)" /> and is uncaught.
	/// The call stack is not unwound at this point (first-chance exception).
	/// Event handlers for this event must be written with care to avoid creating secondary exceptions and to catch any that occur. It is recommended to avoid allocating memory or doing any resource intensive operations in the handler.
	/// The <see cref="E:Avalonia.Threading.Dispatcher.UnhandledExceptionFilter" /> event provides a means to not raise the <see cref="E:Avalonia.Threading.Dispatcher.UnhandledException" /> event. The <see cref="E:Avalonia.Threading.Dispatcher.UnhandledExceptionFilter" /> event is raised first,
	/// and If <see cref="P:Avalonia.Threading.DispatcherUnhandledExceptionFilterEventArgs.RequestCatch" /> is set to false, the <see cref="E:Avalonia.Threading.Dispatcher.UnhandledException" /> event will not be raised.
	/// </remarks>
	public event DispatcherUnhandledExceptionFilterEventHandler? UnhandledExceptionFilter
	{
		add
		{
			_unhandledExceptionFilter = (DispatcherUnhandledExceptionFilterEventHandler)Delegate.Combine(_unhandledExceptionFilter, value);
		}
		remove
		{
			_unhandledExceptionFilter = (DispatcherUnhandledExceptionFilterEventHandler)Delegate.Remove(_unhandledExceptionFilter, value);
		}
	}

	/// <summary>
	///     Raised when the dispatcher is shutting down.
	/// </summary>
	public event EventHandler? ShutdownStarted;

	/// <summary>
	///     Raised when the dispatcher is shut down.
	/// </summary>
	public event EventHandler? ShutdownFinished;

	internal Dispatcher(IDispatcherImpl? impl)
	{
		lock (s_globalLock)
		{
			_thread = System.Threading.Thread.CurrentThread;
			if (FromThread(_thread) != null)
			{
				throw new InvalidOperationException("The current thread already has a dispatcher");
			}
			if (s_uiThread == null)
			{
				s_uiThread = this;
			}
			s_dispatchers.Remove(System.Threading.Thread.CurrentThread);
			s_dispatchers.Add(System.Threading.Thread.CurrentThread, s_currentThreadDispatcher = new DispatcherReferenceStorage
			{
				Reference = new WeakReference<Dispatcher>(this)
			});
		}
		IsSta = _thread.GetApartmentState() == ApartmentState.STA;
		if (impl == null)
		{
			Stopwatch st = Stopwatch.StartNew();
			_timeProvider = () => st.ElapsedMilliseconds;
		}
		else
		{
			_timeProvider = () => impl.Now;
		}
		_impl = null;
		ReplaceImplementation(impl);
		_unhandledExceptionEventArgs = new DispatcherUnhandledExceptionEventArgs(this);
		_exceptionFilterEventArgs = new DispatcherUnhandledExceptionFilterEventArgs(this);
	}

	/// <summary>
	/// Checks that the current thread is the UI thread.
	/// </summary>
	public bool CheckAccess()
	{
		return System.Threading.Thread.CurrentThread == _thread;
	}

	/// <summary>
	/// Checks that the current thread is the UI thread and throws if not.
	/// </summary>
	/// <exception cref="T:System.InvalidOperationException">
	/// The current thread is not the UI thread.
	/// </exception>
	public void VerifyAccess()
	{
		if (!CheckAccess())
		{
			ThrowVerifyAccess();
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		[DoesNotReturn]
		static void ThrowVerifyAccess()
		{
			throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
		}
	}

	internal AvaloniaSynchronizationContext GetContextWithPriority(DispatcherPriority priority)
	{
		DispatcherPriority.Validate(priority, "priority");
		int num = (int)priority - (int)DispatcherPriority.MinValue;
		AvaloniaSynchronizationContext[] priorityContexts = _priorityContexts;
		int num2 = num;
		return priorityContexts[num2] ?? (priorityContexts[num2] = new AvaloniaSynchronizationContext(this, priority));
	}

	/// <summary>
	/// Gets a <see cref="T:System.Threading.Tasks.TaskScheduler" /> which executes tasks on this <see cref="T:Avalonia.Threading.Dispatcher" />. A <see cref="T:Avalonia.Threading.DispatcherPriority" /> is captured from
	/// the current <see cref="T:Avalonia.Threading.AvaloniaSynchronizationContext" /> if one is available. Otherwise, <see cref="F:Avalonia.Threading.DispatcherPriority.Default" /> is used.
	/// </summary>
	public TaskScheduler ToTaskScheduler()
	{
		if (SynchronizationContext.Current is AvaloniaSynchronizationContext avaloniaSynchronizationContext)
		{
			return avaloniaSynchronizationContext.ToTaskScheduler();
		}
		return ToTaskScheduler(DispatcherPriority.Default);
	}

	/// <summary>
	/// Gets a <see cref="T:System.Threading.Tasks.TaskScheduler" /> which executes tasks on this <see cref="T:Avalonia.Threading.Dispatcher" /> with the specified <see cref="T:Avalonia.Threading.DispatcherPriority" />.
	/// </summary>
	public TaskScheduler ToTaskScheduler(DispatcherPriority priority)
	{
		return GetContextWithPriority(priority).ToTaskScheduler();
	}

	private void ReplaceImplementation(IDispatcherImpl? impl)
	{
		using (NonPumpingLockHelper.Use())
		{
			if (impl != null && !impl.CurrentThreadIsLoopThread)
			{
				throw new InvalidOperationException("IDispatcherImpl belongs to a different thread");
			}
			if (_impl != null)
			{
				_impl.Timer -= OnOSTimer;
				_impl.Signaled -= Signaled;
				if (_backgroundProcessingImpl != null)
				{
					_backgroundProcessingImpl.ReadyForBackgroundProcessing -= OnReadyForExplicitBackgroundProcessing;
				}
				_impl = null;
				_controlledImpl = null;
				_pendingInputImpl = null;
				_backgroundProcessingImpl = null;
			}
			if (impl != null)
			{
				_initialized = true;
			}
			else
			{
				impl = new ManagedDispatcherImpl(null);
			}
			_impl = impl;
			impl.Timer += OnOSTimer;
			impl.Signaled += Signaled;
			_controlledImpl = _impl as IControlledDispatcherImpl;
			_pendingInputImpl = _impl as IDispatcherImplWithPendingInput;
			_backgroundProcessingImpl = _impl as IDispatcherImplWithExplicitBackgroundProcessing;
			_maximumInputStarvationTime = ((_backgroundProcessingImpl == null) ? 50 : 50);
			if (_backgroundProcessingImpl != null)
			{
				_backgroundProcessingImpl.ReadyForBackgroundProcessing += OnReadyForExplicitBackgroundProcessing;
			}
			if (_signaled)
			{
				_impl.Signal();
			}
			if (_explicitBackgroundProcessingRequested)
			{
				_backgroundProcessingImpl?.RequestBackgroundProcessing();
			}
			_osTimerSetTo = null;
			UpdateOSTimer();
		}
	}

	/// Exception filter returns true if exception should be caught.
	internal bool ExceptionFilter(Exception e)
	{
		if (!e.Data.Contains(ExceptionDataKey))
		{
			e.Data.Add(ExceptionDataKey, null);
			bool flag = UnhandledException != null;
			if (_unhandledExceptionFilter != null)
			{
				_exceptionFilterEventArgs.Initialize(e, flag);
				bool flag2 = false;
				try
				{
					_unhandledExceptionFilter(this, _exceptionFilterEventArgs);
					flag2 = true;
				}
				finally
				{
					if (flag2)
					{
						flag = _exceptionFilterEventArgs.RequestCatch;
					}
				}
			}
			return flag;
		}
		return false;
	}

	internal bool CatchException(Exception e)
	{
		bool result = false;
		if (UnhandledException != null)
		{
			_unhandledExceptionEventArgs.Initialize(e, handled: false);
			bool flag = false;
			try
			{
				UnhandledException(this, _unhandledExceptionEventArgs);
				result = _unhandledExceptionEventArgs.Handled;
				flag = true;
			}
			finally
			{
				if (!flag)
				{
					result = false;
				}
			}
		}
		return result;
	}

	/// Returns true, if exception was handled.
	internal bool TryCatchWhen(Exception e)
	{
		if (ExceptionFilter(e))
		{
			if (!CatchException(e))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	/// <summary>
	///     Executes the specified Action synchronously on the thread that
	///     the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <remarks>
	///     Note that the default priority is DispatcherPriority.Send.
	/// </remarks>
	public void Invoke(Action callback)
	{
		Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Action synchronously on the thread that
	///     the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	public void Invoke(Action callback, DispatcherPriority priority)
	{
		Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Action synchronously on the thread that
	///     the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Action synchronously on the thread that
	///     the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	/// <param name="timeout">
	///     The minimum amount of time to wait for the operation to start.
	///     Once the operation has started, it will complete before this method
	///     returns.
	/// </param>
	public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken, TimeSpan timeout)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		DispatcherPriority.Validate(priority, "priority");
		if (timeout.TotalMilliseconds < 0.0 && timeout != TimeSpan.FromMilliseconds(-1L))
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		if (!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
		{
			using (AvaloniaSynchronizationContext.Ensure(this, priority))
			{
				callback();
				return;
			}
		}
		DispatcherOperation operation = new DispatcherOperation(this, priority, callback, throwOnUiThread: false);
		InvokeImpl(operation, cancellationToken, timeout);
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; synchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <returns>
	///     The return value from the delegate being invoked.
	/// </returns>
	/// <remarks>
	///     Note that the default priority is DispatcherPriority.Send.
	/// </remarks>
	public TResult Invoke<TResult>(Func<TResult> callback)
	{
		return Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; synchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <returns>
	///     The return value from the delegate being invoked.
	/// </returns>
	public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority)
	{
		return Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; synchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	/// <returns>
	///     The return value from the delegate being invoked.
	/// </returns>
	public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; synchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	/// <param name="timeout">
	///     The minimum amount of time to wait for the operation to start.
	///     Once the operation has started, it will complete before this method
	///     returns.
	/// </param>
	/// <returns>
	///     The return value from the delegate being invoked.
	/// </returns>
	public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken, TimeSpan timeout)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		DispatcherPriority.Validate(priority, "priority");
		if (timeout.TotalMilliseconds < 0.0 && timeout != TimeSpan.FromMilliseconds(-1L))
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		if (!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
		{
			using (AvaloniaSynchronizationContext.Ensure(this, priority))
			{
				return callback();
			}
		}
		DispatcherOperation<TResult> operation = new DispatcherOperation<TResult>(this, priority, callback);
		return (TResult)InvokeImpl(operation, cancellationToken, timeout);
	}

	/// <summary>
	///     Executes the specified Action asynchronously on the thread
	///     that the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	/// <remarks>
	///     Note that the default priority is DispatcherPriority.Default.
	/// </remarks>
	public DispatcherOperation InvokeAsync(Action callback)
	{
		return InvokeAsync(callback, default(DispatcherPriority), CancellationToken.None);
	}

	/// <summary>
	///     Executes the specified Action asynchronously on the thread
	///     that the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority)
	{
		return InvokeAsync(callback, priority, CancellationToken.None);
	}

	/// <summary>
	///     Executes the specified Action asynchronously on the thread
	///     that the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     An Action delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		DispatcherPriority.Validate(priority, "priority");
		DispatcherOperation dispatcherOperation = new DispatcherOperation(this, priority, callback, throwOnUiThread: false);
		InvokeAsyncImpl(dispatcherOperation, cancellationToken);
		return dispatcherOperation;
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; asynchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	/// <remarks>
	///     Note that the default priority is DispatcherPriority.Default.
	/// </remarks>
	public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback)
	{
		return InvokeAsync(callback, DispatcherPriority.Default, CancellationToken.None);
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; asynchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority)
	{
		return InvokeAsync(callback, priority, CancellationToken.None);
	}

	/// <summary>
	///     Executes the specified Func&lt;TResult&gt; asynchronously on the
	///     thread that the Dispatcher was created on.
	/// </summary>
	/// <typeparam name="TResult">The type of the <paramref name="callback" /> return value.</typeparam>
	/// <param name="callback">
	///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token that can be used to cancel the operation.
	///     If the operation has not started, it will be aborted when the
	///     cancellation token is canceled.  If the operation has started,
	///     the operation can cooperate with the cancellation request.
	/// </param>
	/// <returns>
	///     An operation representing the queued delegate to be invoked.
	/// </returns>
	public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		DispatcherPriority.Validate(priority, "priority");
		DispatcherOperation<TResult> dispatcherOperation = new DispatcherOperation<TResult>(this, priority, callback);
		InvokeAsyncImpl(dispatcherOperation, cancellationToken);
		return dispatcherOperation;
	}

	internal void InvokeAsyncImpl(DispatcherOperation operation, CancellationToken cancellationToken)
	{
		bool flag = false;
		lock (InstanceLock)
		{
			if (!cancellationToken.IsCancellationRequested && !_hasShutdownFinished && !Environment.HasShutdownStarted)
			{
				_queue.Enqueue(operation.Priority, operation);
				flag = RequestProcessing();
				if (!flag)
				{
					_queue.RemoveItem(operation);
				}
			}
		}
		if (flag)
		{
			if (cancellationToken.CanBeCanceled)
			{
				CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(delegate(object? s)
				{
					((DispatcherOperation)s).Abort();
				}, operation);
				operation.Aborted += delegate
				{
					cancellationRegistration.Dispose();
				};
				operation.Completed += delegate
				{
					cancellationRegistration.Dispose();
				};
			}
		}
		else
		{
			operation.Status = DispatcherOperationStatus.Aborted;
			operation.CallAbortCallbacks();
		}
	}

	private object? InvokeImpl(DispatcherOperation operation, CancellationToken cancellationToken, TimeSpan timeout)
	{
		object result = null;
		if (!cancellationToken.IsCancellationRequested)
		{
			InvokeAsyncImpl(operation, cancellationToken);
			CancellationToken cancellationToken2 = CancellationToken.None;
			CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
			CancellationTokenSource cancellationTokenSource = null;
			if (timeout.TotalMilliseconds >= 0.0)
			{
				cancellationTokenSource = new CancellationTokenSource(timeout);
				cancellationToken2 = cancellationTokenSource.Token;
				cancellationTokenRegistration = cancellationToken2.Register(delegate(object? s)
				{
					((DispatcherOperation)s).Abort();
				}, operation);
			}
			try
			{
				operation.Wait();
				result = operation.GetResult();
			}
			catch (OperationCanceledException)
			{
				if (cancellationToken2.IsCancellationRequested)
				{
					throw new TimeoutException();
				}
				throw;
			}
			finally
			{
				cancellationTokenRegistration.Dispose();
				cancellationTokenSource?.Dispose();
			}
		}
		return result;
	}

	/// <summary>
	///     Executes the specified Func&lt;Task&gt; asynchronously on the
	///     thread that the Dispatcher was created on
	/// </summary>
	/// <param name="callback">
	///     A Func&lt;Task&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <returns>
	///     An task that completes after the task returned from callback finishes.
	/// </returns>
	public Task InvokeAsync(Func<Task> callback)
	{
		return InvokeAsync(callback, DispatcherPriority.Default);
	}

	/// <summary>
	///     Executes the specified Func&lt;Task&gt; asynchronously on the
	///     thread that the Dispatcher was created on
	/// </summary>
	/// <param name="callback">
	///     A Func&lt;Task&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <returns>
	///     An task that completes after the task returned from callback finishes
	/// </returns>
	public Task InvokeAsync(Func<Task> callback, DispatcherPriority priority)
	{
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		return this.InvokeAsync<Task>(callback, priority).GetTask().Unwrap();
	}

	/// <summary>
	///     Executes the specified Func&lt;Task&lt;TResult&gt;&gt; asynchronously on the
	///     thread that the Dispatcher was created on
	/// </summary>
	/// <param name="action">
	///     A Func&lt;Task&lt;TResult&gt;&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <returns>
	///     An task that completes after the task returned from callback finishes
	/// </returns>
	public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action)
	{
		return InvokeAsync(action, DispatcherPriority.Default);
	}

	/// <summary>
	///     Executes the specified Func&lt;Task&lt;TResult&gt;&gt; asynchronously on the
	///     thread that the Dispatcher was created on
	/// </summary>
	/// <param name="action">
	///     A Func&lt;Task&lt;TResult&gt;&gt; delegate to invoke through the dispatcher.
	/// </param>
	/// <param name="priority">
	///     The priority that determines in what order the specified
	///     callback is invoked relative to the other pending operations
	///     in the Dispatcher.
	/// </param>
	/// <returns>
	///     An task that completes after the task returned from callback finishes
	/// </returns>
	public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action, DispatcherPriority priority)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		return this.InvokeAsync<Task<TResult>>(action, priority).GetTask().Unwrap();
	}

	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread.
	/// </summary>
	/// <param name="action">The method.</param>
	/// <param name="priority">The priority with which to invoke the method.</param>
	public void Post(Action action, DispatcherPriority priority = default(DispatcherPriority))
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		InvokeAsyncImpl(new DispatcherOperation(this, priority, action, throwOnUiThread: true), CancellationToken.None);
	}

	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread.
	/// </summary>
	/// <param name="action">The method.</param>
	/// <param name="arg">The argument of method to call.</param>
	/// <param name="priority">The priority with which to invoke the method.</param>
	public void Post(SendOrPostCallback action, object? arg, DispatcherPriority priority = default(DispatcherPriority))
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		InvokeAsyncImpl(new SendOrPostCallbackDispatcherOperation(this, priority, action, arg, throwOnUiThread: true), CancellationToken.None);
	}

	/// <summary>
	/// Sends an action that will be invoked on the dispatcher thread.
	/// </summary>
	/// <param name="action">The method.</param>
	/// <param name="arg">The argument of method to call.</param>
	/// <param name="priority">The priority with which to invoke the method. If null, Send is default.</param>
	/// <remarks>
	/// When on the same thread with Send priority, callback is executed immediately, without changing synchronization context.
	/// </remarks>
	internal void Send(SendOrPostCallback action, object? arg = null, DispatcherPriority? priority = null)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		DispatcherPriority valueOrDefault = priority.GetValueOrDefault();
		if (!priority.HasValue)
		{
			valueOrDefault = DispatcherPriority.Send;
			priority = valueOrDefault;
		}
		DispatcherPriority? dispatcherPriority = priority;
		valueOrDefault = DispatcherPriority.Send;
		if (dispatcherPriority.HasValue && dispatcherPriority.GetValueOrDefault() == valueOrDefault && CheckAccess())
		{
			try
			{
				using (AvaloniaSynchronizationContext.Ensure(this, priority.Value))
				{
					action(arg);
					return;
				}
			}
			catch (Exception e) when (ExceptionFilter(e))
			{
				if (!CatchException(e))
				{
					throw;
				}
				return;
			}
		}
		InvokeImpl(new SendOrPostCallbackDispatcherOperation(this, priority.Value, action, arg, throwOnUiThread: true), CancellationToken.None, TimeSpan.FromMilliseconds(-1L));
	}

	/// <summary>
	/// Returns a task awaitable that would invoke continuation on specified dispatcher priority
	/// </summary>
	public DispatcherPriorityAwaitable AwaitWithPriority(Task task, DispatcherPriority priority)
	{
		return new DispatcherPriorityAwaitable(this, task, priority);
	}

	/// <summary>
	/// Returns a task awaitable that would invoke continuation on specified dispatcher priority
	/// </summary>
	public DispatcherPriorityAwaitable<T> AwaitWithPriority<T>(Task<T> task, DispatcherPriority priority)
	{
		return new DispatcherPriorityAwaitable<T>(this, task, priority);
	}

	/// <summary>
	/// Creates an awaitable object that asynchronously resumes execution on the dispatcher.
	/// </summary>
	/// <returns>
	/// An awaitable object that asynchronously resumes execution on the dispatcher.
	/// </returns>
	/// <remarks>
	/// This method is equivalent to calling the <see cref="M:Avalonia.Threading.Dispatcher.Resume(Avalonia.Threading.DispatcherPriority)" /> method
	/// and passing in <see cref="F:Avalonia.Threading.DispatcherPriority.Background" />.
	/// </remarks>
	public DispatcherPriorityAwaitable Resume()
	{
		return Resume(DispatcherPriority.Background);
	}

	/// <summary>``
	/// Creates an awaitable object that asynchronously resumes execution on the dispatcher. The work that occurs
	/// when control returns to the code awaiting the result of this method is scheduled with the specified priority.
	/// </summary>
	/// <param name="priority">The priority at which to schedule the continuation.</param>
	/// <returns>
	/// An awaitable object that asynchronously resumes execution on the dispatcher.
	/// </returns>
	public DispatcherPriorityAwaitable Resume(DispatcherPriority priority)
	{
		DispatcherPriority.Validate(priority, "priority");
		return new DispatcherPriorityAwaitable(this, null, priority);
	}

	/// <summary>
	/// Creates an awaitable object that asynchronously yields control back to the current dispatcher
	/// and provides an opportunity for the dispatcher to process other events.
	/// </summary>
	/// <returns>
	/// An awaitable object that asynchronously yields control back to the current dispatcher
	/// and provides an opportunity for the dispatcher to process other events.
	/// </returns>
	/// <remarks>
	/// This method is equivalent to calling the <see cref="M:Avalonia.Threading.Dispatcher.Yield(Avalonia.Threading.DispatcherPriority)" /> method
	/// and passing in <see cref="F:Avalonia.Threading.DispatcherPriority.Background" />.
	/// </remarks>
	/// <exception cref="T:System.InvalidOperationException">
	/// The current thread is not the UI thread.
	/// </exception>
	public static DispatcherPriorityAwaitable Yield()
	{
		return Yield(DispatcherPriority.Background);
	}

	/// <summary>
	/// Creates an cawaitable object that asynchronously yields control back to the current dispatcher
	/// and provides an opportunity for the dispatcher to process other events. The work that occurs when
	/// control returns to the code awaiting the result of this method is scheduled with the specified priority.
	/// </summary>
	/// <param name="priority">The priority at which to schedule the continuation.</param>
	/// <returns>
	/// An awaitable object that asynchronously yields control back to the current dispatcher
	/// and provides an opportunity for the dispatcher to process other events.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The current thread is not the UI thread.
	/// </exception>
	public static DispatcherPriorityAwaitable Yield(DispatcherPriority priority)
	{
		return CurrentDispatcher.Resume(priority);
	}

	/// <summary>
	///     Push an execution frame.
	/// </summary>
	/// <param name="frame">
	///     The frame for the dispatcher to process.
	/// </param>
	public void PushFrame(DispatcherFrame frame)
	{
		VerifyAccess();
		if (_controlledImpl == null)
		{
			throw new PlatformNotSupportedException();
		}
		if (frame == null)
		{
			throw new ArgumentNullException("frame");
		}
		if (_hasShutdownFinished)
		{
			throw new InvalidOperationException("Cannot perform requested operation because the Dispatcher shut down");
		}
		if (DisabledProcessingCount > 0)
		{
			throw new InvalidOperationException("Cannot perform this operation while dispatcher processing is suspended.");
		}
		try
		{
			_frames.Push(frame);
			using (AvaloniaSynchronizationContext.Ensure(this, DispatcherPriority.Normal))
			{
				frame.Run(_controlledImpl);
			}
		}
		finally
		{
			_frames.Pop();
			if (_frames.Count == 0)
			{
				if (HasShutdownStarted)
				{
					ShutdownImpl();
				}
				else
				{
					ExitAllFramesRequested = false;
				}
			}
		}
	}

	/// <summary>
	/// Runs the dispatcher's main loop.
	/// </summary>
	/// <param name="cancellationToken">
	/// A cancellation token used to exit the main loop.
	/// </param>
	public void MainLoop(CancellationToken cancellationToken)
	{
		if (_controlledImpl == null)
		{
			throw new PlatformNotSupportedException();
		}
		DispatcherFrame frame = new DispatcherFrame();
		cancellationToken.Register(delegate
		{
			frame.Continue = false;
		});
		PushFrame(frame);
	}

	/// <summary>
	///     Requests that all nested frames exit.
	/// </summary>
	public void ExitAllFrames()
	{
		if (_frames.Count == 0)
		{
			return;
		}
		ExitAllFramesRequested = true;
		foreach (DispatcherFrame frame in _frames)
		{
			frame.MaybeExitOnDispatcherRequest();
		}
	}

	/// <summary>
	///     Begins the process of shutting down the dispatcher.
	/// </summary>
	public void BeginInvokeShutdown(DispatcherPriority priority)
	{
		Post(StartShutdownImpl, priority);
	}

	/// <summary>
	/// Initiates the shutdown process of the Dispatcher synchronously.
	/// </summary>
	public void InvokeShutdown()
	{
		Invoke(StartShutdownImpl, DispatcherPriority.Send);
	}

	private void StartShutdownImpl()
	{
		if (!_startingShutdown)
		{
			_startingShutdown = true;
			ShutdownStarted?.Invoke(this, EventArgs.Empty);
			HasShutdownStarted = true;
			if (_frames.Count > 0)
			{
				ExitAllFrames();
			}
			else
			{
				ShutdownImpl();
			}
		}
	}

	private void ShutdownImpl()
	{
		DispatcherOperation dispatcherOperation = null;
		_impl.Timer -= OnOSTimer;
		_impl.Signaled -= Signaled;
		do
		{
			lock (InstanceLock)
			{
				if (_queue.MaxPriority != DispatcherPriority.Invalid)
				{
					dispatcherOperation = _queue.Peek();
				}
				else
				{
					dispatcherOperation = null;
					_impl.UpdateTimer(null);
					_hasShutdownFinished = true;
				}
			}
			dispatcherOperation?.Abort();
		}
		while (dispatcherOperation != null);
		ShutdownFinished?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	///     Disable the event processing of the dispatcher.
	/// </summary>
	/// <remarks>
	///     This is an advanced method intended to eliminate the chance of
	///     unrelated reentrancy.  The effect of disabling processing is:
	///     1) CLR locks will not pump messages internally.
	///     2) No one is allowed to push a frame.
	///     3) No message processing is permitted.
	/// </remarks>
	public DispatcherProcessingDisabled DisableProcessing()
	{
		VerifyAccess();
		DisabledProcessingCount++;
		SynchronizationContext current = SynchronizationContext.Current;
		if ((current is AvaloniaSynchronizationContext || current is NonPumpingSyncContext) ? true : false)
		{
			return new DispatcherProcessingDisabled(this);
		}
		NonPumpingLockHelper.IHelperImpl service = AvaloniaLocator.Current.GetService<NonPumpingLockHelper.IHelperImpl>();
		if (service == null)
		{
			return new DispatcherProcessingDisabled(this);
		}
		SynchronizationContext.SetSynchronizationContext(new NonPumpingSyncContext(service, current));
		return new DispatcherProcessingDisabled(this, current);
	}

	private void RequestBackgroundProcessing()
	{
		lock (InstanceLock)
		{
			if (_backgroundProcessingImpl != null)
			{
				if (!_explicitBackgroundProcessingRequested)
				{
					_explicitBackgroundProcessingRequested = true;
					_backgroundProcessingImpl.RequestBackgroundProcessing();
				}
			}
			else if (!_dueTimeForBackgroundProcessing.HasValue)
			{
				_dueTimeForBackgroundProcessing = Now + 1;
				UpdateOSTimer();
			}
		}
	}

	private void OnReadyForExplicitBackgroundProcessing()
	{
		lock (InstanceLock)
		{
			_explicitBackgroundProcessingRequested = false;
		}
		ExecuteJobsCore(fromExplicitBackgroundProcessingCallback: true);
	}

	/// <summary>
	/// Force-runs all dispatcher operations ignoring any pending OS events, use with caution
	/// </summary>
	public void RunJobs(DispatcherPriority? priority = null)
	{
		RunJobs(priority, CancellationToken.None);
	}

	internal void RunJobs(DispatcherPriority? priority, CancellationToken cancellationToken)
	{
		if (DisabledProcessingCount > 0)
		{
			throw new InvalidOperationException("Cannot perform this operation while dispatcher processing is suspended.");
		}
		DispatcherPriority valueOrDefault = priority.GetValueOrDefault();
		if (!priority.HasValue)
		{
			valueOrDefault = DispatcherPriority.MinimumActiveValue;
			priority = valueOrDefault;
		}
		if (priority < DispatcherPriority.MinimumActiveValue)
		{
			priority = DispatcherPriority.MinimumActiveValue;
		}
		while (!cancellationToken.IsCancellationRequested)
		{
			DispatcherOperation dispatcherOperation;
			lock (InstanceLock)
			{
				dispatcherOperation = _queue.Peek();
			}
			if (dispatcherOperation == null || dispatcherOperation.Priority < priority.Value)
			{
				break;
			}
			ExecuteJob(dispatcherOperation);
		}
	}

	internal static void ResetBeforeUnitTests()
	{
		ResetGlobalState();
	}

	internal static void ResetForUnitTests()
	{
		if (s_uiThread == null)
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (true)
		{
			s_uiThread._pendingInputImpl = (s_uiThread._controlledImpl = null);
			s_uiThread._impl = new DummyShuttingDownUnitTestDispatcherImpl();
			if (stopwatch.Elapsed.TotalSeconds > 5.0)
			{
				throw new InvalidProgramException("You've caused dispatcher loop");
			}
			DispatcherOperation dispatcherOperation;
			lock (s_uiThread.InstanceLock)
			{
				dispatcherOperation = s_uiThread._queue.Peek();
			}
			if (dispatcherOperation == null || dispatcherOperation.Priority <= DispatcherPriority.Inactive)
			{
				break;
			}
			s_uiThread.ExecuteJob(dispatcherOperation);
		}
		s_uiThread.ShutdownImpl();
		ResetGlobalState();
	}

	private void ExecuteJob(DispatcherOperation job)
	{
		lock (InstanceLock)
		{
			if (job.Status != DispatcherOperationStatus.Pending)
			{
				return;
			}
			_queue.RemoveItem(job);
			job.Status = DispatcherOperationStatus.Executing;
		}
		job.Execute();
		PromoteTimers();
	}

	private void Signaled()
	{
		lock (InstanceLock)
		{
			_signaled = false;
		}
		ExecuteJobsCore(fromExplicitBackgroundProcessingCallback: false);
	}

	private void ExecuteJobsCore(bool fromExplicitBackgroundProcessingCallback)
	{
		long? num = null;
		while (true)
		{
			DispatcherOperation dispatcherOperation;
			lock (InstanceLock)
			{
				dispatcherOperation = _queue.Peek();
			}
			if (dispatcherOperation == null || dispatcherOperation.Priority < DispatcherPriority.MinimumActiveValue)
			{
				return;
			}
			if (dispatcherOperation.Priority > DispatcherPriority.Input)
			{
				ExecuteJob(dispatcherOperation);
				continue;
			}
			IDispatcherImplWithPendingInput? pendingInputImpl = _pendingInputImpl;
			if (pendingInputImpl != null && pendingInputImpl.CanQueryPendingInput)
			{
				if (!_pendingInputImpl.HasPendingInput)
				{
					ExecuteJob(dispatcherOperation);
					continue;
				}
				RequestBackgroundProcessing();
				return;
			}
			if (_backgroundProcessingImpl != null && !fromExplicitBackgroundProcessingCallback)
			{
				RequestBackgroundProcessing();
				return;
			}
			if (!num.HasValue)
			{
				num = Now;
			}
			if (Now - num.Value > _maximumInputStarvationTime)
			{
				break;
			}
			ExecuteJob(dispatcherOperation);
		}
		RequestBackgroundProcessing();
	}

	internal bool RequestProcessing()
	{
		lock (InstanceLock)
		{
			if (!CheckAccess())
			{
				RequestForegroundProcessing();
				return true;
			}
			if (_queue.MaxPriority <= DispatcherPriority.Input)
			{
				IDispatcherImplWithPendingInput pendingInputImpl = _pendingInputImpl;
				if (pendingInputImpl != null && pendingInputImpl.CanQueryPendingInput && !pendingInputImpl.HasPendingInput)
				{
					RequestForegroundProcessing();
				}
				else
				{
					RequestBackgroundProcessing();
				}
			}
			else
			{
				RequestForegroundProcessing();
			}
		}
		return true;
	}

	private void RequestForegroundProcessing()
	{
		if (!_signaled)
		{
			_signaled = true;
			_impl.Signal();
		}
	}

	internal bool Abort(DispatcherOperation operation)
	{
		lock (InstanceLock)
		{
			if (operation.Status != DispatcherOperationStatus.Pending)
			{
				return false;
			}
			_queue.RemoveItem(operation);
			operation.Status = DispatcherOperationStatus.Aborted;
		}
		return true;
	}

	internal bool SetPriority(DispatcherOperation operation, DispatcherPriority priority)
	{
		bool flag = false;
		lock (InstanceLock)
		{
			if (operation.IsQueued)
			{
				_queue.ChangeItemPriority(operation, priority);
				flag = true;
				if (flag)
				{
					RequestProcessing();
				}
			}
		}
		return flag;
	}

	public bool HasJobsWithPriority(DispatcherPriority priority)
	{
		lock (InstanceLock)
		{
			return _queue.MaxPriority >= priority;
		}
	}

	/// <summary>
	/// Gets all pending jobs, unordered, without removing them.
	/// </summary>
	/// <remarks>Only use between unit tests!</remarks>
	/// <returns>A list of jobs.</returns>
	internal List<DispatcherOperation> GetJobs()
	{
		lock (InstanceLock)
		{
			return _queue.PeekAll();
		}
	}

	/// <summary>
	/// Clears all pending jobs.
	/// </summary>
	/// <remarks>Only use between unit tests!</remarks>
	internal void ClearJobs()
	{
		lock (InstanceLock)
		{
			_queue.Clear();
		}
	}

	public static Dispatcher? FromThread(Thread thread)
	{
		lock (s_globalLock)
		{
			if (s_dispatchers.TryGetValue(thread, out DispatcherReferenceStorage value) && value.Reference.TryGetTarget(out Dispatcher target))
			{
				return target;
			}
			return null;
		}
	}

	internal static Dispatcher? TryGetUIThread()
	{
		lock (s_globalLock)
		{
			return s_uiThread;
		}
	}

	[PrivateApi]
	public static void InitializeUIThreadDispatcher(IPlatformThreadingInterface impl)
	{
		InitializeUIThreadDispatcher(new LegacyDispatcherImpl(impl));
	}

	[PrivateApi]
	public static void InitializeUIThreadDispatcher(IDispatcherImpl impl)
	{
		UIThread.VerifyAccess();
		if (UIThread._initialized)
		{
			throw new InvalidOperationException("UI thread dispatcher is already initialized");
		}
		UIThread.ReplaceImplementation(impl);
	}

	private static void ResetGlobalState()
	{
		lock (s_globalLock)
		{
			foreach (KeyValuePair<Thread, DispatcherReferenceStorage> item in (IEnumerable<KeyValuePair<Thread, DispatcherReferenceStorage>>)s_dispatchers)
			{
				item.Value.Reference = new WeakReference<Dispatcher>(null);
			}
			s_dispatchers.Clear();
			s_currentThreadDispatcher = null;
			s_uiThread = null;
		}
	}

	private void UpdateOSTimer()
	{
		VerifyAccess();
		long? num = ((_dueTimeForTimers.HasValue && _dueTimeForBackgroundProcessing.HasValue) ? new long?(Math.Min(_dueTimeForTimers.Value, _dueTimeForBackgroundProcessing.Value)) : (_dueTimeForTimers ?? _dueTimeForBackgroundProcessing));
		if (_osTimerSetTo != num)
		{
			_impl.UpdateTimer(_osTimerSetTo = num);
		}
	}

	internal void RescheduleTimers()
	{
		if (!CheckAccess())
		{
			Post(RescheduleTimers, DispatcherPriority.Send);
			return;
		}
		lock (InstanceLock)
		{
			if (_hasShutdownFinished)
			{
				return;
			}
			bool dueTimeFound = _dueTimeFound;
			long dueTimeInMs = _dueTimeInMs;
			_dueTimeFound = false;
			_dueTimeInMs = 0L;
			if (_timers.Count > 0)
			{
				for (int i = 0; i < _timers.Count; i++)
				{
					DispatcherTimer dispatcherTimer = _timers[i];
					if (!_dueTimeFound || dispatcherTimer.DueTimeInMs - _dueTimeInMs < 0)
					{
						_dueTimeFound = true;
						_dueTimeInMs = dispatcherTimer.DueTimeInMs;
					}
				}
			}
			if (_dueTimeFound)
			{
				if (!_dueTimeForTimers.HasValue || !dueTimeFound || dueTimeInMs != _dueTimeInMs)
				{
					_dueTimeForTimers = _dueTimeInMs;
					UpdateOSTimer();
				}
			}
			else if (dueTimeFound)
			{
				_dueTimeForTimers = null;
				UpdateOSTimer();
			}
		}
	}

	internal void AddTimer(DispatcherTimer timer)
	{
		lock (InstanceLock)
		{
			if (!_hasShutdownFinished)
			{
				_timers.Add(timer);
				_timersVersion++;
			}
		}
		RescheduleTimers();
	}

	internal void RemoveTimer(DispatcherTimer timer)
	{
		lock (InstanceLock)
		{
			if (!_hasShutdownFinished)
			{
				_timers.Remove(timer);
				_timersVersion++;
			}
		}
		RescheduleTimers();
	}

	private void OnOSTimer()
	{
		_impl.UpdateTimer(null);
		_osTimerSetTo = null;
		bool flag = false;
		bool flag2 = false;
		lock (InstanceLock)
		{
			_impl.UpdateTimer(null);
			_osTimerSetTo = null;
			flag = _dueTimeForTimers.HasValue && _dueTimeForTimers.Value <= Now;
			if (flag)
			{
				_dueTimeForTimers = null;
			}
			flag2 = _dueTimeForBackgroundProcessing.HasValue && _dueTimeForBackgroundProcessing.Value <= Now;
			if (flag2)
			{
				_dueTimeForBackgroundProcessing = null;
			}
		}
		if (flag)
		{
			PromoteTimers();
		}
		if (flag2)
		{
			ExecuteJobsCore(fromExplicitBackgroundProcessingCallback: false);
		}
		UpdateOSTimer();
	}

	internal void PromoteTimers()
	{
		long now = Now;
		try
		{
			List<DispatcherTimer> list = null;
			long num = 0L;
			lock (InstanceLock)
			{
				if (!_hasShutdownFinished && _dueTimeFound && _dueTimeInMs - now <= 0)
				{
					list = _timers;
					num = _timersVersion;
				}
			}
			if (list == null)
			{
				return;
			}
			DispatcherTimer dispatcherTimer = null;
			int i = 0;
			do
			{
				lock (InstanceLock)
				{
					dispatcherTimer = null;
					if (num != _timersVersion)
					{
						num = _timersVersion;
						i = 0;
					}
					for (; i < _timers.Count; i++)
					{
						if (list[i].DueTimeInMs - now <= 0)
						{
							dispatcherTimer = list[i];
							list.RemoveAt(i);
							break;
						}
					}
				}
				dispatcherTimer?.Promote();
			}
			while (dispatcherTimer != null);
		}
		finally
		{
			RescheduleTimers();
		}
	}

	internal static List<DispatcherTimer> SnapshotTimersForUnitTests()
	{
		return s_uiThread._timers.ToList();
	}
}
