using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Threading;

/// <summary>
/// A <see cref="T:System.Threading.SynchronizationContext" /> that uses a <see cref="T:Avalonia.Threading.Dispatcher" /> to post messages.
/// </summary>
public class AvaloniaSynchronizationContext : SynchronizationContext
{
	public record struct RestoreContext : IDisposable
	{
		private readonly SynchronizationContext? _oldContext;

		private bool _needRestore;

		internal RestoreContext(SynchronizationContext? oldContext)
		{
			_oldContext = oldContext;
			_needRestore = true;
		}

		public void Dispose()
		{
			if (_needRestore)
			{
				SynchronizationContext.SetSynchronizationContext(_oldContext);
				_needRestore = false;
			}
		}
	}

	internal readonly DispatcherPriority Priority;

	private readonly NonPumpingLockHelper.IHelperImpl? _nonPumpingHelper = AvaloniaLocator.Current.GetService<NonPumpingLockHelper.IHelperImpl>();

	private readonly Dispatcher _dispatcher;

	private readonly object _taskSchedulerLock = new object();

	private TaskScheduler? _taskScheduler;

	/// <summary>
	/// Controls if SynchronizationContext should be installed in InstallIfNeeded. Used by Designer.
	/// </summary>
	public static bool AutoInstall { get; set; } = true;

	internal AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority, bool isStaThread)
	{
		_dispatcher = dispatcher;
		Priority = priority;
		if ((_nonPumpingHelper != null) & isStaThread)
		{
			SetWaitNotificationRequired();
		}
	}

	public AvaloniaSynchronizationContext()
		: this(Dispatcher.CurrentDispatcher, DispatcherPriority.Default)
	{
	}

	public AvaloniaSynchronizationContext(DispatcherPriority priority)
		: this(Dispatcher.CurrentDispatcher, priority)
	{
	}

	public AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority)
		: this(dispatcher, priority, dispatcher.IsSta)
	{
	}

	/// <summary>
	/// Installs synchronization context in current thread
	/// </summary>
	public static void InstallIfNeeded()
	{
		if (AutoInstall && !(SynchronizationContext.Current is AvaloniaSynchronizationContext))
		{
			SynchronizationContext.SetSynchronizationContext(Dispatcher.CurrentDispatcher.GetContextWithPriority(DispatcherPriority.Normal));
		}
	}

	/// <inheritdoc />
	public override void Post(SendOrPostCallback d, object? state)
	{
		_dispatcher.Post(d, state, Priority);
	}

	/// <inheritdoc />
	public override void Send(SendOrPostCallback d, object? state)
	{
		if (_dispatcher.CheckAccess())
		{
			_dispatcher.Send(d, state, DispatcherPriority.Send);
		}
		else
		{
			_dispatcher.Send(d, state, Priority);
		}
	}

	public override int Wait(nint[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		if (_nonPumpingHelper != null && _dispatcher.CheckAccess() && _dispatcher.DisabledProcessingCount > 0)
		{
			return _nonPumpingHelper.Wait(waitHandles, waitAll, millisecondsTimeout);
		}
		return base.Wait(waitHandles, waitAll, millisecondsTimeout);
	}

	/// <summary>
	/// Gets a <see cref="T:System.Threading.Tasks.TaskScheduler" /> associated with this <see cref="T:Avalonia.Threading.AvaloniaSynchronizationContext" />.
	/// </summary>
	public TaskScheduler ToTaskScheduler()
	{
		lock (_taskSchedulerLock)
		{
			if (_taskScheduler == null)
			{
				SynchronizationContext current = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(this);
				try
				{
					_taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
				}
				finally
				{
					SynchronizationContext.SetSynchronizationContext(current);
				}
			}
			return _taskScheduler;
		}
	}

	public static RestoreContext Ensure(DispatcherPriority priority)
	{
		return Ensure(Dispatcher.CurrentDispatcher, priority);
	}

	public static RestoreContext Ensure(Dispatcher dispatcher, DispatcherPriority priority)
	{
		if (SynchronizationContext.Current is AvaloniaSynchronizationContext avaloniaSynchronizationContext && avaloniaSynchronizationContext.Priority == priority)
		{
			return default(RestoreContext);
		}
		SynchronizationContext? current = SynchronizationContext.Current;
		dispatcher.VerifyAccess();
		SynchronizationContext.SetSynchronizationContext(dispatcher.GetContextWithPriority(priority));
		return new RestoreContext(current);
	}
}
