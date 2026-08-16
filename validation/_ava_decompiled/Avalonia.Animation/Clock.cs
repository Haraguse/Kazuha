using System;
using Avalonia.Reactive;

namespace Avalonia.Animation;

internal class Clock : ClockBase
{
	private readonly IDisposable _parentSubscription;

	public static IClock GlobalClock => AvaloniaLocator.Current.GetRequiredService<IGlobalClock>();

	public Clock()
		: this(GlobalClock)
	{
	}

	public Clock(IClock parent)
	{
		_parentSubscription = parent.Subscribe(base.Pulse);
	}

	protected override void Stop()
	{
		_parentSubscription?.Dispose();
	}
}
