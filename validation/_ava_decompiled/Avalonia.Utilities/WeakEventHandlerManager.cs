using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Manages subscriptions to events using weak listeners.
/// </summary>
public static class WeakEventHandlerManager
{
	private static class SubscriptionTypeStorage<TArgs, TSubscriber> where TArgs : EventArgs where TSubscriber : class
	{
		public static readonly ConditionalWeakTable<object, SubscriptionDic<TArgs, TSubscriber>> Subscribers = new ConditionalWeakTable<object, SubscriptionDic<TArgs, TSubscriber>>();
	}

	private class SubscriptionDic<T, TSubscriber> : Dictionary<string, Subscription<T, TSubscriber>> where T : EventArgs where TSubscriber : class
	{
	}

	private class Subscription<T, TSubscriber> where T : EventArgs where TSubscriber : class
	{
		private delegate void CallerDelegate(TSubscriber s, object? sender, T args);

		private struct Descriptor
		{
			public WeakReference<TSubscriber>? Subscriber;

			public CallerDelegate? Caller;
		}

		private readonly EventInfo _info;

		private readonly SubscriptionDic<T, TSubscriber> _sdic;

		private readonly object _target;

		private readonly string _eventName;

		private readonly Delegate _delegate;

		private Descriptor[] _data = new Descriptor[2];

		private int _count;

		private static readonly Dictionary<MethodInfo, CallerDelegate> s_callers = new Dictionary<MethodInfo, CallerDelegate>();

		public Subscription(SubscriptionDic<T, TSubscriber> sdic, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] Type targetType, object target, string eventName)
		{
			_sdic = sdic;
			_target = target;
			_eventName = eventName;
			if (!s_accessors.TryGetValue(targetType, out Dictionary<string, EventInfo> value))
			{
				value = (s_accessors[targetType] = new Dictionary<string, EventInfo>());
			}
			if (value.TryGetValue(eventName, out var value2))
			{
				_info = value2;
			}
			else
			{
				EventInfo eventInfo = targetType.GetRuntimeEvents().FirstOrDefault((EventInfo x) => x.Name == eventName);
				if (eventInfo == null)
				{
					throw new ArgumentException($"The event {eventName} was not found on {target.GetType()}.");
				}
				value[eventName] = (_info = eventInfo);
			}
			Action<object, T> action = OnEvent;
			_delegate = action.GetMethodInfo().CreateDelegate(_info.EventHandlerType, action.Target);
			_info.AddMethod.Invoke(target, new object[1] { _delegate });
		}

		private void Destroy()
		{
			_info.RemoveMethod.Invoke(_target, new object[1] { _delegate });
			_sdic.Remove(_eventName);
		}

		public void Add(EventHandler<T> s)
		{
			Compact(preventDestroy: true);
			if (_count == _data.Length)
			{
				Descriptor[] array = new Descriptor[_data.Length * 2];
				Array.Copy(_data, array, _data.Length);
				_data = array;
			}
			MethodInfo method = s.Method;
			TSubscriber target = (TSubscriber)s.Target;
			if (!s_callers.TryGetValue(method, out CallerDelegate value))
			{
				value = (s_callers[method] = (CallerDelegate)Delegate.CreateDelegate(typeof(CallerDelegate), null, method));
			}
			_data[_count] = new Descriptor
			{
				Caller = value,
				Subscriber = new WeakReference<TSubscriber>(target)
			};
			_count++;
		}

		public void Remove(EventHandler<T> s)
		{
			bool flag = false;
			for (int i = 0; i < _count; i++)
			{
				WeakReference<TSubscriber> subscriber = _data[i].Subscriber;
				if (subscriber != null && subscriber.TryGetTarget(out var target) && object.Equals(target, s.Target))
				{
					_data[i] = default(Descriptor);
					flag = true;
				}
			}
			if (flag)
			{
				Compact();
			}
		}

		private void Compact(bool preventDestroy = false)
		{
			int num = -1;
			for (int i = 0; i < _count; i++)
			{
				Descriptor descriptor = _data[i];
				TSubscriber target = null;
				descriptor.Subscriber?.TryGetTarget(out target);
				if (target == null && num == -1)
				{
					num = i;
				}
				if (target != null && num != -1)
				{
					_data[i] = default(Descriptor);
					_data[num] = descriptor;
					num++;
				}
			}
			if (num != -1)
			{
				_count = num;
			}
			if (_count == 0 && !preventDestroy)
			{
				Destroy();
			}
		}

		private void OnEvent(object? sender, T eventArgs)
		{
			bool flag = false;
			for (int i = 0; i < _count; i++)
			{
				if (_data[i].Subscriber.TryGetTarget(out var target))
				{
					_data[i].Caller(target, sender, eventArgs);
				}
				else
				{
					flag = true;
				}
			}
			if (flag)
			{
				Compact();
			}
		}
	}

	private static readonly Dictionary<Type, Dictionary<string, EventInfo>> s_accessors = new Dictionary<Type, Dictionary<string, EventInfo>>();

	/// <summary>
	/// Subscribes to an event on an object using a weak subscription.
	/// </summary>
	/// <typeparam name="TTarget">The type of the target.</typeparam>
	/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
	/// <typeparam name="TSubscriber">The type of the subscriber.</typeparam>
	/// <param name="target">The event source.</param>
	/// <param name="eventName">The name of the event.</param>
	/// <param name="subscriber">The subscriber.</param>
	public static void Subscribe<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] TTarget, TEventArgs, TSubscriber>(TTarget target, string eventName, EventHandler<TEventArgs> subscriber) where TEventArgs : EventArgs where TSubscriber : class
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		SubscriptionDic<TEventArgs, TSubscriber> orCreateValue = SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.GetOrCreateValue(target);
		if (!orCreateValue.TryGetValue(eventName, out Subscription<TEventArgs, TSubscriber> value))
		{
			value = (orCreateValue[eventName] = new Subscription<TEventArgs, TSubscriber>(orCreateValue, typeof(TTarget), target, eventName));
		}
		value.Add(subscriber);
	}

	/// <summary>
	/// Unsubscribes from an event.
	/// </summary>
	/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
	/// <typeparam name="TSubscriber">The type of the subscriber.</typeparam>
	/// <param name="target">The event source.</param>
	/// <param name="eventName">The name of the event.</param>
	/// <param name="subscriber">The subscriber.</param>
	public static void Unsubscribe<TEventArgs, TSubscriber>(object target, string eventName, EventHandler<TEventArgs> subscriber) where TEventArgs : EventArgs where TSubscriber : class
	{
		if (SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.TryGetValue(target, out SubscriptionDic<TEventArgs, TSubscriber> value) && value.TryGetValue(eventName, out Subscription<TEventArgs, TSubscriber> value2))
		{
			value2.Remove(subscriber);
		}
	}
}
