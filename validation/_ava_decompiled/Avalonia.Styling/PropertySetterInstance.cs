using System;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Styling;

/// <summary>
/// A <see cref="T:Avalonia.Styling.Setter" /> which has been instanced on a control.
/// </summary>
/// <typeparam name="T">The target property type.</typeparam>
internal class PropertySetterInstance<T> : SingleSubscriberObservableBase<BindingValue<T>>, ISetterInstance
{
	private enum State
	{
		Inactive,
		Active,
		Disposed
	}

	private readonly StyledElement _target;

	private readonly StyledProperty<T>? _styledProperty;

	private readonly DirectPropertyBase<T>? _directProperty;

	private readonly T _value;

	private IDisposable? _subscription;

	private State _state;

	private bool IsActive => _state == State.Active;

	public PropertySetterInstance(StyledElement target, StyledProperty<T> property, T value)
	{
		_target = target;
		_styledProperty = property;
		_value = value;
	}

	public PropertySetterInstance(StyledElement target, DirectPropertyBase<T> property, T value)
	{
		_target = target;
		_directProperty = property;
		_value = value;
	}

	public void Start(bool hasActivator)
	{
		if (hasActivator)
		{
			if ((object)_styledProperty != null)
			{
				_subscription = _target.Bind(_styledProperty, this, BindingPriority.StyleTrigger);
			}
			else
			{
				_subscription = _target.Bind(_directProperty, this);
			}
			return;
		}
		AvaloniaObject target = _target;
		if ((object)_styledProperty != null)
		{
			_subscription = target.SetValue(_styledProperty, _value, BindingPriority.Style);
		}
		else
		{
			target.SetValue(_directProperty, _value);
		}
	}

	public void Activate()
	{
		if (!IsActive)
		{
			_state = State.Active;
			PublishNext();
		}
	}

	public void Deactivate()
	{
		if (IsActive)
		{
			_state = State.Inactive;
			PublishNext();
		}
	}

	public override void Dispose()
	{
		if (_state == State.Disposed)
		{
			return;
		}
		_state = State.Disposed;
		if (_subscription != null)
		{
			IDisposable? subscription = _subscription;
			_subscription = null;
			subscription.Dispose();
		}
		else if (IsActive)
		{
			if ((object)_styledProperty != null)
			{
				_target.ClearValue(_styledProperty);
			}
			else
			{
				_target.ClearValue(_directProperty);
			}
		}
		base.Dispose();
	}

	protected override void Subscribed()
	{
		PublishNext();
	}

	protected override void Unsubscribed()
	{
	}

	private void PublishNext()
	{
		PublishNext(IsActive ? new BindingValue<T>(_value) : default(BindingValue<T>));
	}
}
