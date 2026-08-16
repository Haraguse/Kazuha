using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore;

internal abstract class BindingEntryBase<TValue, TSource> : IValueEntry<TValue>, IValueEntry, IObserver<TSource>, IObserver<BindingValue<TSource>>, IDisposable
{
	private class UncommonFields
	{
		public TValue? _defaultValue;

		public bool _isDefaultValueInitialized;

		public bool _hasDataValidation;

		public BindingValueType _dataValidationState;

		public Exception? _dataValidationError;
	}

	private IDisposable? _subscription;

	private bool _hasValue;

	private TValue? _value;

	private UncommonFields? _uncommon;

	public bool IsSubscribed => _subscription != null;

	public AvaloniaProperty Property { get; }

	AvaloniaProperty IValueEntry.Property => Property;

	protected ValueFrame Frame { get; }

	protected internal object Source { get; }

	protected BindingEntryBase(AvaloniaObject target, ValueFrame frame, AvaloniaProperty property, IObservable<BindingValue<TSource>> source)
		: this(target, frame, property, (object)source)
	{
	}

	protected BindingEntryBase(AvaloniaObject target, ValueFrame frame, AvaloniaProperty property, IObservable<TSource> source)
		: this(target, frame, property, (object)source)
	{
	}

	private BindingEntryBase(AvaloniaObject target, ValueFrame frame, AvaloniaProperty property, object source)
	{
		Frame = frame;
		Property = property;
		Source = source;
		if (property.GetMetadata(target).EnableDataValidation == true)
		{
			_uncommon = new UncommonFields
			{
				_hasDataValidation = true
			};
		}
	}

	public void Dispose()
	{
		Unsubscribe();
		BindingCompleted();
	}

	public bool HasValue()
	{
		Start(produceValue: false);
		return _hasValue;
	}

	public TValue GetValue()
	{
		Start(produceValue: false);
		if (!_hasValue)
		{
			throw new AvaloniaInternalException("The binding entry has no value.");
		}
		return _value;
	}

	public bool GetDataValidationState(out BindingValueType state, out Exception? error)
	{
		UncommonFields? uncommon = _uncommon;
		if (uncommon != null && uncommon._hasDataValidation)
		{
			state = _uncommon._dataValidationState;
			error = _uncommon._dataValidationError;
			return true;
		}
		state = BindingValueType.Value;
		error = null;
		return false;
	}

	public void Start()
	{
		Start(produceValue: true);
	}

	public void OnCompleted()
	{
		BindingCompleted();
	}

	public void OnError(Exception error)
	{
		BindingCompleted();
	}

	public void OnNext(TSource value)
	{
		SetValue(ConvertAndValidate(value));
	}

	public void OnNext(BindingValue<TSource> value)
	{
		SetValue(ConvertAndValidate(value));
	}

	public virtual void Unsubscribe()
	{
		_subscription?.Dispose();
		_subscription = null;
	}

	object? IValueEntry.GetValue()
	{
		Start(produceValue: false);
		if (!_hasValue)
		{
			throw new AvaloniaInternalException("The BindingEntry<T> has no value.");
		}
		return _value;
	}

	protected abstract BindingValue<TValue> ConvertAndValidate(TSource value);

	protected abstract BindingValue<TValue> ConvertAndValidate(BindingValue<TSource> value);

	protected abstract TValue GetDefaultValue(AvaloniaObject owner);

	protected virtual void Start(bool produceValue)
	{
		if (_subscription != null)
		{
			return;
		}
		_subscription = (produceValue ? BindingEntryBaseNonGenericHelper.Creating : BindingEntryBaseNonGenericHelper.CreatingQuiet);
		object source = Source;
		IDisposable subscription;
		if (!(source is IObservable<BindingValue<TSource>> observable))
		{
			if (!(source is IObservable<TSource> observable2))
			{
				throw new AvaloniaInternalException("Unexpected binding source.");
			}
			subscription = observable2.Subscribe(this);
		}
		else
		{
			subscription = observable.Subscribe(this);
		}
		_subscription = subscription;
	}

	private void SetValue(BindingValue<TValue> value)
	{
		if (value.Type == BindingValueType.DoNothing)
		{
			return;
		}
		if (Dispatcher.UIThread.CheckAccess())
		{
			Execute(this, value);
			return;
		}
		BindingValue<TValue> newValue = value;
		Dispatcher.UIThread.Post(delegate
		{
			Execute(this, newValue);
		});
		static void Execute(BindingEntryBase<TValue, TSource> instance, BindingValue<TValue> bindingValue)
		{
			ValueStore owner = instance.Frame.Owner;
			if (owner != null)
			{
				_ = owner.Owner;
				_ = instance.Property;
				_ = bindingValue.Type;
				if (!bindingValue.HasValue && bindingValue.Type != BindingValueType.DataValidationError)
				{
					bindingValue = bindingValue.WithValue(instance.GetCachedDefaultValue());
				}
				UncommonFields? uncommon = instance._uncommon;
				if (uncommon != null && uncommon._hasDataValidation)
				{
					instance._uncommon._dataValidationState = bindingValue.Type;
					instance._uncommon._dataValidationError = bindingValue.Error;
				}
				if (bindingValue.HasValue && (!instance._hasValue || !EqualityComparer<TValue>.Default.Equals(instance._value, bindingValue.Value)))
				{
					instance._value = bindingValue.Value;
					instance._hasValue = true;
					if (instance._subscription != null && instance._subscription != BindingEntryBaseNonGenericHelper.CreatingQuiet)
					{
						instance.Frame.Owner?.OnBindingValueChanged(instance, instance.Frame.Priority);
					}
				}
			}
		}
	}

	private void BindingCompleted()
	{
		_subscription = null;
		Frame.OnBindingCompleted(this);
	}

	private TValue GetCachedDefaultValue()
	{
		UncommonFields? uncommon = _uncommon;
		if (uncommon == null || !uncommon._isDefaultValueInitialized)
		{
			if (_uncommon == null)
			{
				_uncommon = new UncommonFields();
			}
			_uncommon._defaultValue = GetDefaultValue(Frame.Owner.Owner);
			_uncommon._isDefaultValueInitialized = true;
		}
		return _uncommon._defaultValue;
	}
}
