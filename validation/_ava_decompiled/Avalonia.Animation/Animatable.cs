using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Animation;

/// <summary>
/// Base class for all animatable objects.
/// </summary>
public class Animatable : AvaloniaObject
{
	private class TransitionState
	{
		public IDisposable? Instance { get; set; }

		public object? BaseValue { get; set; }
	}

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animatable.Clock" /> property.
	/// </summary>
	internal static readonly StyledProperty<IClock?> ClockProperty = AvaloniaProperty.Register<Animatable, IClock>("Clock", null, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Animation.Animatable.Transitions" /> property.
	/// </summary>
	public static readonly StyledProperty<Transitions?> TransitionsProperty = AvaloniaProperty.Register<Animatable, Transitions>("Transitions");

	private bool _transitionsEnabled = true;

	private bool _isSubscribedToTransitionsCollection;

	private Dictionary<ITransition, TransitionState>? _transitionState;

	private NotifyCollectionChangedEventHandler? _collectionChanged;

	private NotifyCollectionChangedEventHandler TransitionsCollectionChangedHandler => TransitionsCollectionChanged;

	/// <summary>
	/// Gets or sets the clock which controls the animations on the control.
	/// </summary>
	internal IClock? Clock
	{
		get
		{
			return GetValue(ClockProperty);
		}
		set
		{
			SetValue(ClockProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the property transitions for the control.
	/// </summary>
	public Transitions? Transitions
	{
		get
		{
			return GetValue(TransitionsProperty);
		}
		set
		{
			SetValue(TransitionsProperty, value);
		}
	}

	/// <summary>
	/// Enables transitions for the control.
	/// </summary>
	/// <remarks>
	/// This method should not be called from user code, it will be called automatically by the framework
	/// when a control is added to the visual tree.
	/// </remarks>
	internal void EnableTransitions()
	{
		if (_transitionsEnabled)
		{
			return;
		}
		_transitionsEnabled = true;
		Transitions transitions = Transitions;
		if (transitions != null)
		{
			if (!_isSubscribedToTransitionsCollection)
			{
				_isSubscribedToTransitionsCollection = true;
				transitions.CollectionChanged += TransitionsCollectionChangedHandler;
			}
			AddTransitions(transitions);
		}
	}

	/// <summary>
	/// Disables transitions for the control.
	/// </summary>
	/// <remarks>
	/// This method should not be called from user code, it will be called automatically by the framework
	/// when a control is removed from the visual tree.
	/// </remarks>
	internal void DisableTransitions()
	{
		if (!_transitionsEnabled)
		{
			return;
		}
		_transitionsEnabled = false;
		Transitions transitions = Transitions;
		if (transitions != null)
		{
			if (_isSubscribedToTransitionsCollection)
			{
				_isSubscribedToTransitionsCollection = false;
				transitions.CollectionChanged -= TransitionsCollectionChangedHandler;
			}
			RemoveTransitions(transitions);
		}
	}

	protected sealed override void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == TransitionsProperty && change.IsEffectiveValueChange)
		{
			AvaloniaPropertyChangedEventArgs<Transitions> obj = (AvaloniaPropertyChangedEventArgs<Transitions>)change;
			Transitions valueOrDefault = obj.OldValue.GetValueOrDefault();
			Transitions valueOrDefault2 = obj.NewValue.GetValueOrDefault();
			if (valueOrDefault2 != null)
			{
				IList items = valueOrDefault2;
				if (valueOrDefault2.Count > 0 && valueOrDefault != null && valueOrDefault.Count > 0)
				{
					items = valueOrDefault2.Except(valueOrDefault).ToList();
				}
				if (_transitionsEnabled)
				{
					valueOrDefault2.CollectionChanged += TransitionsCollectionChangedHandler;
					_isSubscribedToTransitionsCollection = true;
				}
				AddTransitions(items);
			}
			if (valueOrDefault != null)
			{
				IList items2 = valueOrDefault;
				if (valueOrDefault.Count > 0 && valueOrDefault2 != null && valueOrDefault2.Count > 0)
				{
					items2 = valueOrDefault.Except(valueOrDefault2).ToList();
				}
				valueOrDefault.CollectionChanged -= TransitionsCollectionChangedHandler;
				RemoveTransitions(items2);
			}
		}
		else if (_transitionsEnabled)
		{
			Transitions transitions = Transitions;
			if (transitions != null && _transitionState != null && !change.Property.IsDirect && change.Priority > BindingPriority.Animation)
			{
				for (int num = transitions.Count - 1; num >= 0; num--)
				{
					ITransition transition = transitions[num];
					if (transition.Property == change.Property && _transitionState.TryGetValue(transition, out TransitionState value))
					{
						object obj2 = value.BaseValue;
						object animationBaseValue = GetAnimationBaseValue(transition.Property);
						if (!object.Equals(obj2, animationBaseValue))
						{
							value.BaseValue = animationBaseValue;
							object value2 = GetValue(transition.Property);
							if (!object.Equals(animationBaseValue, value2))
							{
								obj2 = value2;
							}
							IClock clock = Clock ?? AvaloniaLocator.Current.GetRequiredService<IGlobalClock>();
							value.Instance?.Dispose();
							value.Instance = transition.Apply(this, clock, obj2, animationBaseValue);
							return;
						}
					}
				}
			}
		}
		base.OnPropertyChangedCore(change);
	}

	private void TransitionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_transitionsEnabled)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Add:
				AddTransitions(e.NewItems);
				break;
			case NotifyCollectionChangedAction.Remove:
				RemoveTransitions(e.OldItems);
				break;
			case NotifyCollectionChangedAction.Replace:
				RemoveTransitions(e.OldItems);
				AddTransitions(e.NewItems);
				break;
			case NotifyCollectionChangedAction.Reset:
				throw new NotSupportedException("Transitions collection cannot be reset.");
			case NotifyCollectionChangedAction.Move:
				break;
			}
		}
	}

	private void AddTransitions(IList items)
	{
		if (_transitionsEnabled)
		{
			if (_transitionState == null)
			{
				_transitionState = new Dictionary<ITransition, TransitionState>();
			}
			for (int i = 0; i < items.Count; i++)
			{
				ITransition transition = (ITransition)items[i];
				_transitionState.Add(transition, new TransitionState
				{
					BaseValue = GetAnimationBaseValue(transition.Property)
				});
			}
		}
	}

	private void RemoveTransitions(IList items)
	{
		if (_transitionState == null)
		{
			return;
		}
		for (int i = 0; i < items.Count; i++)
		{
			ITransition key = (ITransition)items[i];
			if (_transitionState.TryGetValue(key, out TransitionState value))
			{
				value.Instance?.Dispose();
				_transitionState.Remove(key);
			}
		}
	}

	private object? GetAnimationBaseValue(AvaloniaProperty property)
	{
		object obj = this.GetBaseValue(property);
		if (obj == AvaloniaProperty.UnsetValue)
		{
			obj = GetValue(property);
		}
		return obj;
	}

	/// <summary>
	/// For unit tests only!
	/// </summary>
	internal TransitionInstance? TryGetTransitionInstance<TValue>(Transition<TValue> transition)
	{
		if (_transitionState != null && _transitionState.TryGetValue(transition, out TransitionState value))
		{
			return ((value.Instance as BindingEntryBase<TValue, TValue>)?.Source as TransitionObservableBase<TValue>)?.Progress as TransitionInstance;
		}
		return null;
	}
}
