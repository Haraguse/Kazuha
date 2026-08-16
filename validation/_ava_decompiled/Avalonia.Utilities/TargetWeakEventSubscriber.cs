using System;

namespace Avalonia.Utilities;

public sealed class TargetWeakEventSubscriber<TTarget, TEventArgs> : IWeakEventSubscriber<TEventArgs>
{
	private readonly TTarget _target;

	private readonly Action<TTarget, object?, WeakEvent, TEventArgs> _dispatchFunc;

	public TargetWeakEventSubscriber(TTarget target, Action<TTarget, object?, WeakEvent, TEventArgs> dispatchFunc)
	{
		_target = target;
		_dispatchFunc = dispatchFunc;
	}

	void IWeakEventSubscriber<TEventArgs>.OnEvent(object? sender, WeakEvent ev, TEventArgs e)
	{
		_dispatchFunc(_target, sender, ev, e);
	}
}
