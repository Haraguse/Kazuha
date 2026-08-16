using System;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Threading;

internal class NonPumpingSyncContext : SynchronizationContext, IDisposable
{
	private readonly NonPumpingLockHelper.IHelperImpl _impl;

	private readonly SynchronizationContext? _inner;

	public NonPumpingSyncContext(NonPumpingLockHelper.IHelperImpl impl, SynchronizationContext? inner)
	{
		_impl = impl;
		_inner = inner;
		SetWaitNotificationRequired();
		SynchronizationContext.SetSynchronizationContext(this);
	}

	public override void Post(SendOrPostCallback d, object? state)
	{
		if (_inner == null)
		{
			ThreadPool.QueueUserWorkItem(delegate((SendOrPostCallback d, object state) x)
			{
				x.d(x.state);
			}, (d, state), preferLocal: false);
		}
		else
		{
			_inner.Post(d, state);
		}
	}

	public override void Send(SendOrPostCallback d, object? state)
	{
		if (_inner == null)
		{
			d(state);
		}
		else
		{
			_inner.Send(d, state);
		}
	}

	public override int Wait(nint[] waitHandles, bool waitAll, int millisecondsTimeout)
	{
		return _impl.Wait(waitHandles, waitAll, millisecondsTimeout);
	}

	public void Dispose()
	{
		SynchronizationContext.SetSynchronizationContext(_inner);
	}

	internal static IDisposable? Use(NonPumpingLockHelper.IHelperImpl impl)
	{
		SynchronizationContext current = SynchronizationContext.Current;
		if (current == null && Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
		{
			return null;
		}
		if (current is NonPumpingSyncContext)
		{
			return null;
		}
		return new NonPumpingSyncContext(impl, current);
	}
}
