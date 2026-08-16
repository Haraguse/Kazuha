using System;
using System.Collections.Generic;
using Avalonia.Layout;

namespace Avalonia.Interactivity;

/// <summary>
/// Base class for objects that raise routed events.
/// </summary>
public class Interactive : Layoutable
{
	private readonly struct EventSubscription(Delegate handler, RoutingStrategies routes, bool handledEventsToo, Action<Delegate, object, RoutedEventArgs>? invokeAdapter = null)
	{
		public Action<Delegate, object, RoutedEventArgs>? InvokeAdapter { get; } = invokeAdapter;

		public Delegate Handler { get; } = handler;

		public RoutingStrategies Routes { get; } = routes;

		public bool HandledEventsToo { get; } = handledEventsToo;
	}

	private Dictionary<RoutedEvent, List<EventSubscription>>? _eventHandlers;

	internal static int TotalHandlersCount { get; private set; }

	/// <summary>
	/// Gets the interactive parent of the object for bubbling and tunneling events.
	/// </summary>
	internal virtual Interactive? InteractiveParent => base.VisualParent as Interactive;

	/// <summary>
	/// Adds a handler for the specified routed event.
	/// </summary>
	/// <param name="routedEvent">The routed event.</param>
	/// <param name="handler">The handler.</param>
	/// <param name="routes">The routing strategies to listen to.</param>
	/// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
	public void AddHandler(RoutedEvent routedEvent, Delegate handler, RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble, bool handledEventsToo = false)
	{
		routedEvent = routedEvent ?? throw new ArgumentNullException("routedEvent");
		handler = handler ?? throw new ArgumentNullException("handler");
		EventSubscription subscription = new EventSubscription(handler, routes, handledEventsToo);
		AddEventSubscription(routedEvent, subscription);
	}

	/// <summary>
	/// Adds a handler for the specified routed event.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
	/// <param name="routedEvent">The routed event.</param>
	/// <param name="handler">The handler.</param>
	/// <param name="routes">The routing strategies to listen to.</param>
	/// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
	public void AddHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs>? handler, RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble, bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
	{
		routedEvent = routedEvent ?? throw new ArgumentNullException("routedEvent");
		if (handler != null)
		{
			EventSubscription subscription = new EventSubscription(handler, routes, handledEventsToo, delegate(Delegate baseHandler, object sender, RoutedEventArgs args)
			{
				InvokeAdapter(baseHandler, sender, args);
			});
			AddEventSubscription(routedEvent, subscription);
		}
		static void InvokeAdapter(Delegate baseHandler, object sender, RoutedEventArgs args)
		{
			EventHandler<TEventArgs> obj = (EventHandler<TEventArgs>)baseHandler;
			TEventArgs e = (TEventArgs)args;
			obj(sender, e);
		}
	}

	/// <summary>
	/// Removes a handler for the specified routed event.
	/// </summary>
	/// <param name="routedEvent">The routed event.</param>
	/// <param name="handler">The handler.</param>
	public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
	{
		routedEvent = routedEvent ?? throw new ArgumentNullException("routedEvent");
		handler = handler ?? throw new ArgumentNullException("handler");
		if (_eventHandlers == null || !_eventHandlers.TryGetValue(routedEvent, out List<EventSubscription> value))
		{
			return;
		}
		for (int num = value.Count - 1; num >= 0; num--)
		{
			if (value[num].Handler == handler)
			{
				value.RemoveAt(num);
				TotalHandlersCount--;
			}
		}
	}

	/// <summary>
	/// Removes a handler for the specified routed event.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
	/// <param name="routedEvent">The routed event.</param>
	/// <param name="handler">The handler.</param>
	public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs>? handler) where TEventArgs : RoutedEventArgs
	{
		if (handler != null)
		{
			RemoveHandler((RoutedEvent)routedEvent, (Delegate)handler);
		}
	}

	/// <summary>
	/// Raises a routed event.
	/// </summary>
	/// <param name="e">The event args.</param>
	public void RaiseEvent(RoutedEventArgs e)
	{
		e = e ?? throw new ArgumentNullException("e");
		if (e.RoutedEvent == null)
		{
			throw new ArgumentException("Cannot raise an event whose RoutedEvent is null.");
		}
		using EventRoute eventRoute = BuildEventRoute(e.RoutedEvent);
		eventRoute.RaiseEvent(this, e);
	}

	/// <summary>
	/// Builds an event route for a routed event.
	/// </summary>
	/// <param name="e">The routed event.</param>
	/// <returns>An <see cref="T:Avalonia.Interactivity.EventRoute" /> describing the route.</returns>
	/// <remarks>
	/// Usually, calling <see cref="M:Avalonia.Interactivity.Interactive.RaiseEvent(Avalonia.Interactivity.RoutedEventArgs)" /> is sufficient to raise a routed
	/// event, however there are situations in which the construction of the event args is expensive
	/// and should be avoided if there are no handlers for an event. In these cases you can call
	/// this method to build the event route and check the <see cref="P:Avalonia.Interactivity.EventRoute.HasHandlers" />
	/// property to see if there are any handlers registered on the route. If there are, call
	/// <see cref="M:Avalonia.Interactivity.EventRoute.RaiseEvent(Avalonia.Interactivity.Interactive,Avalonia.Interactivity.RoutedEventArgs)" /> to raise the event.
	/// </remarks>
	protected EventRoute BuildEventRoute(RoutedEvent e)
	{
		e = e ?? throw new ArgumentNullException("e");
		EventRoute eventRoute = new EventRoute(e);
		bool hasRaisedSubscriptions = e.HasRaisedSubscriptions;
		if (e.RoutingStrategies.HasAllFlags(RoutingStrategies.Bubble) || e.RoutingStrategies.HasAllFlags(RoutingStrategies.Tunnel))
		{
			for (Interactive interactive = this; interactive != null; interactive = interactive.InteractiveParent)
			{
				if (hasRaisedSubscriptions)
				{
					eventRoute.AddClassHandler(interactive);
				}
				interactive.AddToEventRoute(e, eventRoute);
			}
		}
		else
		{
			if (hasRaisedSubscriptions)
			{
				eventRoute.AddClassHandler(this);
			}
			AddToEventRoute(e, eventRoute);
		}
		return eventRoute;
	}

	private void AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
	{
		if (_eventHandlers == null)
		{
			_eventHandlers = new Dictionary<RoutedEvent, List<EventSubscription>>();
		}
		if (!_eventHandlers.TryGetValue(routedEvent, out List<EventSubscription> value))
		{
			value = new List<EventSubscription>();
			_eventHandlers.Add(routedEvent, value);
		}
		value.Add(subscription);
		TotalHandlersCount++;
	}

	private void AddToEventRoute(RoutedEvent routedEvent, EventRoute route)
	{
		routedEvent = routedEvent ?? throw new ArgumentNullException("routedEvent");
		route = route ?? throw new ArgumentNullException("route");
		if (_eventHandlers == null || !_eventHandlers.TryGetValue(routedEvent, out List<EventSubscription> value))
		{
			return;
		}
		foreach (EventSubscription item in value)
		{
			route.Add(this, item.Handler, item.Routes, item.HandledEventsToo, item.InvokeAdapter);
		}
	}
}
