using System;

namespace Avalonia.Utilities;

public sealed class WeakEventSubscriber<TEventArgs> : IWeakEventSubscriber<TEventArgs>
{
	public event Action<object?, WeakEvent, TEventArgs>? Event;

	void IWeakEventSubscriber<TEventArgs>.OnEvent(object? sender, WeakEvent ev, TEventArgs e)
	{
		Event?.Invoke(sender, ev, e);
	}
}
