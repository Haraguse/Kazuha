using System;
using Avalonia.Reactive;

namespace Avalonia.Interactivity;

public class RoutedEvent
{
	private readonly LightweightSubject<(object, RoutedEventArgs)> _raised = new LightweightSubject<(object, RoutedEventArgs)>();

	private readonly LightweightSubject<RoutedEventArgs> _routeFinished = new LightweightSubject<RoutedEventArgs>();

	public Type EventArgsType { get; }

	public string Name { get; }

	public Type OwnerType { get; }

	public RoutingStrategies RoutingStrategies { get; }

	public bool HasRaisedSubscriptions => _raised.HasObservers;

	public IObservable<(object, RoutedEventArgs)> Raised => _raised;

	public IObservable<RoutedEventArgs> RouteFinished => _routeFinished;

	public RoutedEvent(string name, RoutingStrategies routingStrategies, Type eventArgsType, Type ownerType)
	{
		name = name ?? throw new ArgumentNullException("name");
		eventArgsType = eventArgsType ?? throw new ArgumentNullException("name");
		ownerType = ownerType ?? throw new ArgumentNullException("name");
		if (!typeof(RoutedEventArgs).IsAssignableFrom(eventArgsType))
		{
			throw new InvalidCastException("eventArgsType must be derived from RoutedEventArgs.");
		}
		EventArgsType = eventArgsType;
		Name = name;
		OwnerType = ownerType;
		RoutingStrategies = routingStrategies;
	}

	public static RoutedEvent<TEventArgs> Register<TOwner, TEventArgs>(string name, RoutingStrategies routingStrategy) where TEventArgs : RoutedEventArgs
	{
		name = name ?? throw new ArgumentNullException("name");
		RoutedEvent<TEventArgs> routedEvent = new RoutedEvent<TEventArgs>(name, routingStrategy, typeof(TOwner));
		RoutedEventRegistry.Instance.Register(typeof(TOwner), routedEvent);
		return routedEvent;
	}

	public static RoutedEvent<TEventArgs> Register<TEventArgs>(string name, RoutingStrategies routingStrategy, Type ownerType) where TEventArgs : RoutedEventArgs
	{
		name = name ?? throw new ArgumentNullException("name");
		RoutedEvent<TEventArgs> routedEvent = new RoutedEvent<TEventArgs>(name, routingStrategy, ownerType);
		RoutedEventRegistry.Instance.Register(ownerType, routedEvent);
		return routedEvent;
	}

	public IDisposable AddClassHandler(Type targetType, EventHandler<RoutedEventArgs> handler, RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble, bool handledEventsToo = false)
	{
		return Raised.Subscribe<(object, RoutedEventArgs)>(delegate((object, RoutedEventArgs) args)
		{
			var (obj, e) = args;
			if (targetType.IsInstanceOfType(obj) && (e.Route == RoutingStrategies.Direct || (e.Route & routes) != 0) && (!e.Handled | handledEventsToo))
			{
				handler(obj, e);
			}
		});
	}

	internal void InvokeRaised(object sender, RoutedEventArgs e)
	{
		_raised.OnNext((sender, e));
	}

	internal void InvokeRouteFinished(RoutedEventArgs e)
	{
		_routeFinished.OnNext(e);
	}

	public override string ToString()
	{
		return FormattableString.Invariant($"{OwnerType.Name}.{Name}");
	}
}
public class RoutedEvent<TEventArgs> : RoutedEvent where TEventArgs : RoutedEventArgs
{
	public RoutedEvent(string name, RoutingStrategies routingStrategies, Type ownerType)
		: base(name, routingStrategies, typeof(TEventArgs), ownerType)
	{
	}

	public IDisposable AddClassHandler<TTarget>(Action<TTarget, TEventArgs> handler, RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble, bool handledEventsToo = false) where TTarget : Interactive
	{
		return AddClassHandler(typeof(TTarget), Adapter, routes, handledEventsToo);
		void Adapter(object? sender, RoutedEventArgs e)
		{
			if (sender is TTarget arg && e is TEventArgs arg2)
			{
				handler(arg, arg2);
			}
		}
	}
}
