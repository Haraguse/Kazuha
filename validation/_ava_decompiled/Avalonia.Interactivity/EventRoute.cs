using System;
using Avalonia.Collections.Pooled;
using Avalonia.Diagnostics;

namespace Avalonia.Interactivity;

/// <summary>
/// Holds the route for a routed event and supports raising an event on that route.
/// </summary>
public class EventRoute : IDisposable
{
	private readonly struct RouteItem(Interactive target, Delegate? handler, Action<Delegate, object, RoutedEventArgs>? adapter, RoutingStrategies routes, bool handledEventsToo)
	{
		public Interactive Target { get; } = target;

		public Delegate? Handler { get; } = handler;

		public Action<Delegate, object, RoutedEventArgs>? Adapter { get; } = adapter;

		public RoutingStrategies Routes { get; } = routes;

		public bool HandledEventsToo { get; } = handledEventsToo;
	}

	private readonly RoutedEvent _event;

	private PooledList<RouteItem>? _route;

	/// <summary>
	/// Gets a value indicating whether the route has any handlers.
	/// </summary>
	public bool HasHandlers
	{
		get
		{
			PooledList<RouteItem>? route = _route;
			if (route == null)
			{
				return false;
			}
			return route.Count > 0;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Interactivity.RoutedEvent" /> class.
	/// </summary>
	/// <param name="e">The routed event to be raised.</param>
	public EventRoute(RoutedEvent e)
	{
		e = e ?? throw new ArgumentNullException("e");
		_event = e;
		_route = null;
	}

	/// <summary>
	/// Adds a handler to the route.
	/// </summary>
	/// <param name="target">The target on which the event should be raised.</param>
	/// <param name="handler">The handler for the event.</param>
	/// <param name="routes">The routing strategies to listen to.</param>
	/// <param name="handledEventsToo">
	/// If true the handler will be raised even when the routed event is marked as handled.
	/// </param>
	/// <param name="adapter">
	/// An optional adapter which if supplied, will be called with <paramref name="handler" />
	/// and the parameters for the event. This adapter can be used to avoid calling
	/// `DynamicInvoke` on the handler.
	/// </param>
	public void Add(Interactive target, Delegate handler, RoutingStrategies routes, bool handledEventsToo = false, Action<Delegate, object, RoutedEventArgs>? adapter = null)
	{
		target = target ?? throw new ArgumentNullException("target");
		handler = handler ?? throw new ArgumentNullException("handler");
		if (_route == null)
		{
			_route = new PooledList<RouteItem>(16);
		}
		_route.Add(new RouteItem(target, handler, adapter, routes, handledEventsToo));
	}

	/// <summary>
	/// Adds a class handler to the route.
	/// </summary>
	/// <param name="target">The target on which the event should be raised.</param>
	public void AddClassHandler(Interactive target)
	{
		target = target ?? throw new ArgumentNullException("target");
		if (_route == null)
		{
			_route = new PooledList<RouteItem>(16);
		}
		_route.Add(new RouteItem(target, null, null, (RoutingStrategies)0, handledEventsToo: false));
	}

	/// <summary>
	/// Raises an event along the route.
	/// </summary>
	/// <param name="source">The event source.</param>
	/// <param name="e">The event args.</param>
	public void RaiseEvent(Interactive source, RoutedEventArgs e)
	{
		source = source ?? throw new ArgumentNullException("source");
		e = e ?? throw new ArgumentNullException("e");
		e.Source = source;
		using (Diagnostic.RaisingRoutedEvent()?.AddTag("Control", e.Source).AddTag("RoutedEvent", e.RoutedEvent))
		{
			if (_event.RoutingStrategies == RoutingStrategies.Direct)
			{
				e.Route = RoutingStrategies.Direct;
				RaiseEventImpl(e);
				_event.InvokeRouteFinished(e);
				return;
			}
			if (_event.RoutingStrategies.HasAllFlags(RoutingStrategies.Tunnel))
			{
				e.Route = RoutingStrategies.Tunnel;
				RaiseEventImpl(e);
				_event.InvokeRouteFinished(e);
			}
			if (_event.RoutingStrategies.HasAllFlags(RoutingStrategies.Bubble))
			{
				e.Route = RoutingStrategies.Bubble;
				RaiseEventImpl(e);
				_event.InvokeRouteFinished(e);
			}
		}
	}

	/// <summary>
	/// Disposes of the event route.
	/// </summary>
	public void Dispose()
	{
		_route?.Dispose();
		_route = null;
	}

	private void RaiseEventImpl(RoutedEventArgs e)
	{
		if (_route == null)
		{
			return;
		}
		Interactive interactive = null;
		int num = 0;
		int num2 = _route.Count;
		int num3 = 1;
		if (e.Route == RoutingStrategies.Tunnel)
		{
			num = num2 - 1;
			num3 = (num2 = -1);
		}
		for (int i = num; i != num2; i += num3)
		{
			RouteItem routeItem = _route[i];
			if (routeItem.Target != interactive)
			{
				_event.InvokeRaised(routeItem.Target, e);
				if (e.Route == RoutingStrategies.Direct && interactive != null)
				{
					break;
				}
				interactive = routeItem.Target;
			}
			if ((object)routeItem.Handler != null && routeItem.Routes.HasAllFlags(e.Route) && (!e.Handled || routeItem.HandledEventsToo))
			{
				if (routeItem.Adapter != null)
				{
					routeItem.Adapter(routeItem.Handler, routeItem.Target, e);
					continue;
				}
				routeItem.Handler.DynamicInvoke(routeItem.Target, e);
			}
		}
	}
}
