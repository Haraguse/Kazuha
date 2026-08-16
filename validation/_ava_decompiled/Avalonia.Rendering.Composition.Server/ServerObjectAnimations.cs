using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerObjectAnimations
{
	private class ServerObjectSubscriptionStore
	{
		public bool IsValid = true;

		public RefTrackingDictionary<IAnimationInstance>? Subscribers;

		public void Invalidate()
		{
			if (!IsValid)
			{
				return;
			}
			IsValid = false;
			if (Subscribers == null)
			{
				return;
			}
			foreach (KeyValuePair<IAnimationInstance, int> subscriber in Subscribers)
			{
				subscriber.Key.Invalidate();
			}
		}
	}

	private abstract class ServerObjectAnimationInstance
	{
		private ExpressionVariant _cachedVariant;

		public ServerObjectAnimations Owner { get; }

		public bool IsDirty { get; set; } = true;

		public bool NeedsUpdate { get; set; } = true;

		public IAnimationInstance Animation { get; }

		public ServerObjectAnimationInstance(ServerObjectAnimations owner, IAnimationInstance animation)
		{
			Animation = animation;
			Owner = owner;
		}

		public ExpressionVariant GetVariant()
		{
			_ = Owner._owner.Compositor;
			if (!IsDirty)
			{
				return _cachedVariant;
			}
			IsDirty = false;
			return _cachedVariant = Animation.Evaluate(Owner._owner.Compositor.ServerNow, _cachedVariant);
		}

		public abstract void UpdateTargetProperty();
	}

	private class ServerObjectAnimationInstance<T> : ServerObjectAnimationInstance where T : struct
	{
		private readonly CompositionProperty<T> _property;

		public ServerObjectAnimationInstance(ServerObjectAnimations owner, IAnimationInstance animation, CompositionProperty<T> property)
			: base(owner, animation)
		{
			_property = property;
		}

		public override void UpdateTargetProperty()
		{
			if (base.NeedsUpdate)
			{
				base.NeedsUpdate = false;
				_property.SetField(base.Owner._owner, GetVariant().CastOrDefault<T>());
				base.Owner._owner.NotifyAnimatedValueChanged(_property);
				base.Owner.OnSetDirectValue(_property);
			}
		}
	}

	private readonly ServerObject _owner;

	private InlineDictionary<CompositionProperty, ServerObjectSubscriptionStore> _subscriptions;

	private InlineDictionary<CompositionProperty, ServerObjectAnimationInstance> _animations;

	public ServerObjectAnimations(ServerObject owner)
	{
		_owner = owner;
	}

	public void Activated()
	{
		foreach (KeyValuePair<CompositionProperty, ServerObjectAnimationInstance> animation in _animations)
		{
			animation.Value.Animation.Activate();
		}
	}

	public void Deactivated()
	{
		foreach (KeyValuePair<CompositionProperty, ServerObjectAnimationInstance> animation in _animations)
		{
			animation.Value.Animation.Deactivate();
		}
	}

	public void OnSetDirectValue(CompositionProperty property)
	{
		if (_subscriptions.TryGetValue(property, out ServerObjectSubscriptionStore value))
		{
			value.Invalidate();
		}
	}

	public void OnSetAnimatedValue<T>(CompositionProperty<T> prop, ref T field, TimeSpan committedAt, IAnimationInstance animation) where T : struct
	{
		if (_owner.IsActive && _animations.TryGetValue(prop, out ServerObjectAnimationInstance value))
		{
			value.Animation.Deactivate();
		}
		_animations[prop] = new ServerObjectAnimationInstance<T>(this, animation, prop);
		animation.Initialize(committedAt, ExpressionVariant.Create(field), prop);
		if (_owner.IsActive)
		{
			animation.Activate();
		}
		OnSetDirectValue(prop);
	}

	public void RemoveAnimationForProperty(CompositionProperty property)
	{
		if (_animations.TryGetAndRemoveValue(property, out ServerObjectAnimationInstance value) && _owner.IsActive)
		{
			value.Animation.Deactivate();
		}
		OnSetDirectValue(property);
	}

	public void SubscribeToInvalidation(CompositionProperty member, IAnimationInstance animation)
	{
		if (!_subscriptions.TryGetValue(member, out ServerObjectSubscriptionStore value))
		{
			value = (_subscriptions[member] = new ServerObjectSubscriptionStore());
		}
		if (value.Subscribers == null)
		{
			value.Subscribers = new RefTrackingDictionary<IAnimationInstance>();
		}
		value.Subscribers.AddRef(animation);
	}

	public void UnsubscribeFromInvalidation(CompositionProperty member, IAnimationInstance animation)
	{
		if (_subscriptions.TryGetValue(member, out ServerObjectSubscriptionStore value))
		{
			value.Subscribers?.ReleaseRef(animation);
		}
	}

	public ExpressionVariant GetPropertyForAnimation(string name)
	{
		CompositionProperty compositionProperty = _owner.GetCompositionProperty(name);
		if (compositionProperty == null)
		{
			return default(ExpressionVariant);
		}
		if (_subscriptions.TryGetValue(compositionProperty, out ServerObjectSubscriptionStore value))
		{
			value.IsValid = true;
		}
		if (_animations.TryGetValue(compositionProperty, out ServerObjectAnimationInstance value2))
		{
			return value2.GetVariant();
		}
		return compositionProperty.GetVariant?.Invoke(_owner) ?? default(ExpressionVariant);
	}

	public void EvaluateAnimations()
	{
		foreach (KeyValuePair<CompositionProperty, ServerObjectAnimationInstance> animation in _animations)
		{
			if (animation.Value.IsDirty)
			{
				animation.Value.UpdateTargetProperty();
			}
		}
	}

	public void NotifyAnimationInstanceInvalidated(CompositionProperty property)
	{
		if (_animations.TryGetValue(property, out ServerObjectAnimationInstance value))
		{
			ServerObjectAnimationInstance serverObjectAnimationInstance = value;
			bool isDirty = (value.NeedsUpdate = true);
			serverObjectAnimationInstance.IsDirty = isDirty;
			_owner.Compositor.Animations.AddDirtyAnimatedObject(this);
		}
	}
}
