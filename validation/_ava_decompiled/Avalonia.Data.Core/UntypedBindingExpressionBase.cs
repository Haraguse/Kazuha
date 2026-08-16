using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Logging;
using Avalonia.Metadata;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// Base class for binding expressions which produce untyped values.
/// </summary>
[PrivateApi]
public abstract class UntypedBindingExpressionBase : BindingExpressionBase, IDisposable, IDescription, IValueEntry
{
	private sealed class ObservableSink : LightweightObservableBase<object?>, IBindingExpressionSink, IAvaloniaSubject<object?>, IObserver<object?>, IObservable<object?>
	{
		private readonly UntypedBindingExpressionBase _expression;

		private object? _value = AvaloniaProperty.UnsetValue;

		public ObservableSink(UntypedBindingExpressionBase expression)
		{
			_expression = expression;
		}

		void IBindingExpressionSink.OnChanged(UntypedBindingExpressionBase instance, bool hasValueChanged, bool hasErrorChanged, object? value, BindingError? error)
		{
			if (instance.IsDataValidationEnabled || error != null)
			{
				BindingNotification value2 = ((error != null && error.ErrorType == BindingErrorType.Error) ? new BindingNotification(error.Exception, BindingErrorType.Error, value) : ((error == null || error.ErrorType != BindingErrorType.DataValidationError) ? new BindingNotification(value) : new BindingNotification(error.Exception, BindingErrorType.DataValidationError, value)));
				PublishNext(value2);
			}
			else if (hasValueChanged)
			{
				PublishNext(value);
			}
		}

		void IBindingExpressionSink.OnCompleted(UntypedBindingExpressionBase instance)
		{
			PublishCompleted();
		}

		void IObserver<object?>.OnCompleted()
		{
		}

		void IObserver<object?>.OnError(Exception error)
		{
		}

		void IObserver<object?>.OnNext(object? value)
		{
			_expression.WriteValueToSource(value);
		}

		protected override void Initialize()
		{
			_expression.Start(produceValue: true);
		}

		protected override void Deinitialize()
		{
			_expression.Stop();
		}

		protected override void Subscribed(IObserver<object> observer, bool first)
		{
			if (!first && _value != AvaloniaProperty.UnsetValue)
			{
				base.PublishNext(_value);
			}
		}

		private new void PublishNext(object? value)
		{
			_value = value;
			base.PublishNext(value);
		}
	}

	protected static readonly object UnchangedValue = new object();

	private readonly bool _isDataValidationEnabled;

	private object? _defaultValue;

	private BindingError? _error;

	private ImmediateValueFrame? _frame;

	private bool _isDefaultValueInitialized;

	private bool _isRunning;

	private bool _produceValue;

	private IBindingExpressionSink? _sink;

	private WeakReference<AvaloniaObject?>? _target;

	private object? _value = AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets a description of the binding expression.
	/// </summary>
	public abstract string Description { get; }

	/// <summary>
	/// Gets the current error state of the binding expression.
	/// </summary>
	public BindingErrorType ErrorType => _error?.ErrorType ?? BindingErrorType.None;

	/// <summary>
	/// Gets a value indicating whether data validation is enabled for the binding expression.
	/// </summary>
	public bool IsDataValidationEnabled => _isDataValidationEnabled;

	/// <summary>
	/// Gets a value indicating whether the binding expression is currently running.
	/// </summary>
	public bool IsRunning => _isRunning;

	/// <summary>
	/// Gets the priority of the binding expression.
	/// </summary>
	/// <remarks>
	/// Before being attached to a value store, this property describes the default priority of the
	/// binding expression; this may change when the expression is attached to a value store.
	/// </remarks>
	public BindingPriority Priority { get; private set; }

	/// <summary>
	/// Gets the <see cref="T:Avalonia.AvaloniaProperty" /> which the binding expression is targeting.
	/// </summary>
	public AvaloniaProperty? TargetProperty { get; private set; }

	/// <summary>
	/// Gets the target type of the binding expression; that is, the type that values produced by
	/// the expression should be converted to.
	/// </summary>
	public Type TargetType { get; private set; }

	AvaloniaProperty IValueEntry.Property => TargetProperty ?? throw new Exception();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Core.UntypedBindingExpressionBase" /> class.
	/// </summary>
	/// <param name="defaultPriority">
	/// The default binding priority for the expression.
	/// </param>
	/// <param name="targetProperty">The target property being bound to.</param>
	/// <param name="isDataValidationEnabled">Whether data validation is enabled.</param>
	public UntypedBindingExpressionBase(BindingPriority defaultPriority, AvaloniaProperty? targetProperty = null, bool isDataValidationEnabled = false)
	{
		Priority = defaultPriority;
		TargetProperty = targetProperty;
		TargetType = targetProperty?.PropertyType ?? typeof(object);
		_isDataValidationEnabled = isDataValidationEnabled;
	}

	/// <summary>
	/// Terminates the binding.
	/// </summary>
	public override void Dispose()
	{
		if (_sink != null)
		{
			Stop();
			IBindingExpressionSink sink = _sink;
			ImmediateValueFrame? frame = _frame;
			_sink = null;
			_frame = null;
			sink.OnCompleted(this);
			frame?.OnEntryDisposed(this);
		}
	}

	/// <summary>
	/// Gets the current value of the binding expression.
	/// </summary>
	/// <returns>
	/// The current value or <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> if the binding was unable
	/// to read a value.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The binding expression has not been started.
	/// </exception>
	public object? GetValue()
	{
		if (!IsRunning)
		{
			throw new InvalidOperationException("BindingExpression has not been started.");
		}
		return _value;
	}

	/// <summary>
	/// Gets the current value of the binding expression or the default value for the target property.
	/// </summary>
	/// <returns>
	/// The current value or the target property default.
	/// </returns>
	public object? GetValueOrDefault()
	{
		object obj = GetValue();
		if (obj == AvaloniaProperty.UnsetValue)
		{
			obj = GetCachedDefaultValue();
		}
		return obj;
	}

	/// <summary>
	/// Starts the binding expression following a call to <see cref="M:Avalonia.Data.Core.UntypedBindingExpressionBase.AttachCore(Avalonia.Data.Core.IBindingExpressionSink,Avalonia.PropertyStore.ImmediateValueFrame,Avalonia.AvaloniaObject,Avalonia.AvaloniaProperty,Avalonia.Data.BindingPriority)" />.
	/// </summary>
	public void Start()
	{
		Start(produceValue: true);
	}

	bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
	{
		if (_error != null)
		{
			state = _error.ErrorType switch
			{
				BindingErrorType.Error => BindingValueType.BindingError, 
				BindingErrorType.DataValidationError => BindingValueType.DataValidationError, 
				_ => throw new InvalidOperationException("Invalid BindingErrorType."), 
			};
			error = _error.Exception;
		}
		else
		{
			state = BindingValueType.Value;
			error = null;
		}
		return IsDataValidationEnabled;
	}

	bool IValueEntry.HasValue()
	{
		Start(produceValue: false);
		return true;
	}

	object? IValueEntry.GetValue()
	{
		Start(produceValue: false);
		return GetValueOrDefault();
	}

	void IValueEntry.Unsubscribe()
	{
		Stop();
	}

	internal override void Attach(ValueStore valueStore, ImmediateValueFrame? frame, AvaloniaObject target, AvaloniaProperty targetProperty, BindingPriority priority)
	{
		AttachCore(valueStore, frame, target, targetProperty, priority);
	}

	/// <summary>
	/// Initializes the binding expression with the specified subscriber and target property and
	/// starts it.
	/// </summary>
	/// <param name="subscriber">The subscriber.</param>
	/// <param name="target">The target object.</param>
	/// <param name="targetProperty">The target property.</param>
	/// <param name="priority">The priority of the binding.</param>
	internal void AttachAndStart(IBindingExpressionSink subscriber, AvaloniaObject target, AvaloniaProperty? targetProperty, BindingPriority priority)
	{
		AttachCore(subscriber, null, target, targetProperty, priority);
		Start(produceValue: true);
	}

	/// <summary>
	/// Produces an observable which can be used to observe the value of the binding expression.
	/// </summary>
	/// <param name="target">The binding target, if known.</param>
	/// <returns>An observable subject.</returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The binding expression is already instantiated on an AvaloniaObject.
	/// </exception>
	/// <remarks>
	/// This method is mostly here for unit testing and we may want to remove it in future. In
	/// particular its usefulness is limited in that it preserves the semantics of binding
	/// expressions as expected by unit tests, not necessarily the semantics that will be used
	/// when the expression is used as an <see cref="T:Avalonia.PropertyStore.IValueEntry" /> instantiated in a
	/// <see cref="T:Avalonia.PropertyStore.ValueStore" />. Unit tests should be migrated to not test the behaviour of
	/// binding expressions through an observable, and instead test the behaviour of the binding
	/// when applied to an <see cref="T:Avalonia.AvaloniaObject" />.
	///
	/// A binding expression may only act as an observable or as a binding expression targeting an
	/// AvaloniaObject, not both.
	/// </remarks>
	internal IAvaloniaSubject<object?> ToObservable(AvaloniaObject? target = null)
	{
		if (_sink is ObservableSink result)
		{
			return result;
		}
		if (_sink != null)
		{
			throw new InvalidOperationException("Cannot call AsObservable on a to binding expression which is already instantiated on an AvaloniaObject.");
		}
		ObservableSink result2 = (ObservableSink)(_sink = new ObservableSink(this));
		_target = ((target != null) ? new WeakReference<AvaloniaObject>(target) : null);
		return result2;
	}

	/// <summary>
	/// When overridden in a derived class, writes the specified value to the binding source if
	/// possible.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns>
	/// True if the value could be written to the binding source; otherwise false.
	/// </returns>
	internal virtual bool WriteValueToSource(object? value)
	{
		return false;
	}

	private void AttachCore(IBindingExpressionSink sink, ImmediateValueFrame? frame, AvaloniaObject target, AvaloniaProperty? targetProperty, BindingPriority priority)
	{
		if (_sink != null)
		{
			throw new InvalidOperationException("BindingExpression was already attached.");
		}
		if ((object)TargetProperty != null && TargetProperty != targetProperty)
		{
			throw new InvalidOperationException("BindingExpression was already attached to a different property.");
		}
		_sink = sink;
		_frame = frame;
		_target = new WeakReference<AvaloniaObject>(target);
		TargetProperty = targetProperty;
		TargetType = targetProperty?.PropertyType ?? typeof(object);
		Priority = priority;
	}

	/// <summary>
	/// Converts a value using a value converter, logging a warning if necessary.
	/// </summary>
	/// <param name="converter">The value converter.</param>
	/// <param name="converterCulture">The culture to use for the conversion.</param>
	/// <param name="converterParameter">The converter parameter.</param>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The target type to convert to.</param>
	/// <param name="error">The current error state.</param>
	/// <returns>
	/// The converted value, or <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> if an error occurred;
	/// in which case the error state will logged and updated in <paramref name="error" />.
	/// </returns>
	private protected object? Convert(IValueConverter converter, CultureInfo? converterCulture, object? converterParameter, object? value, Type targetType, ref BindingError? error)
	{
		try
		{
			return converter.Convert(value, targetType, converterParameter, converterCulture ?? CultureInfo.CurrentCulture);
		}
		catch (Exception ex)
		{
			string value2 = value?.ToString() ?? "(null)";
			string value3 = value?.GetType().FullName ?? "null";
			string text = $"Could not convert '{value2}' ({value3}) to '{targetType}' using '{converter}'";
			if (ShouldLogError(out AvaloniaObject target))
			{
				Log(target, text + ": " + ex.Message);
			}
			error = new BindingError(new InvalidCastException(text + ".", ex), BindingErrorType.Error);
			return AvaloniaProperty.UnsetValue;
		}
	}

	/// <summary>
	/// Converts a value using a value converter's ConvertBack method, logging a warning if
	/// necessary.
	/// </summary>
	/// <param name="converter">The value converter.</param>
	/// <param name="converterCulture">The culture to use for the conversion.</param>
	/// <param name="converterParameter">The converter parameter.</param>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The target type to convert to.</param>
	/// <returns>
	/// The converted value, or <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> if an error occurred;
	/// in which case the error will be logged.
	/// </returns>
	protected object? ConvertBack(IValueConverter converter, CultureInfo? converterCulture, object? converterParameter, object? value, Type targetType)
	{
		try
		{
			return converter.ConvertBack(value, targetType, converterParameter, converterCulture ?? CultureInfo.CurrentCulture);
		}
		catch (Exception ex)
		{
			string value2 = value?.ToString() ?? "(null)";
			string value3 = value?.GetType().FullName ?? "null";
			string text = $"Could not convert '{value2}' ({value3}) to '{targetType}' using '{converter}'";
			if (ShouldLogError(out AvaloniaObject target))
			{
				Log(target, text + ": " + ex.Message);
			}
			return AvaloniaProperty.UnsetValue;
		}
	}

	/// <summary>
	/// Logs a binding error.
	/// </summary>
	/// <param name="error">The error message.</param>
	/// <param name="level">The log level.</param>
	protected void Log(string error, LogEventLevel level = LogEventLevel.Warning)
	{
		if (TryGetTarget(out AvaloniaObject target) && Logger.TryGet(level, "Binding", out var outLogger))
		{
			outLogger.Log(target, "An error occurred binding {Property} to {Expression}: {Message}", ((object)TargetProperty) ?? ((object)"(unknown)"), Description, error);
		}
	}

	/// <summary>
	/// Logs a binding error.
	/// </summary>
	/// <param name="target">The target of the binding expression.</param>
	/// <param name="error">The error message.</param>
	/// <param name="level">The log level.</param>
	protected void Log(AvaloniaObject target, string error, LogEventLevel level = LogEventLevel.Warning)
	{
		if (Logger.TryGet(level, "Binding", out var outLogger))
		{
			outLogger.Log(target, "An error occurred binding {Property} to {Expression}: {Message}", ((object)TargetProperty) ?? ((object)"(unknown)"), Description, error);
		}
	}

	/// <summary>
	/// Publishes a new value and/or error state to the target.
	/// </summary>
	/// <param name="value">The new value, or <see cref="F:Avalonia.Data.Core.UntypedBindingExpressionBase.UnchangedValue" />.</param>
	/// <param name="error">The new binding or data validation error.</param>
	/// <param name="forceUpdate">If true, forces the value to be published even if it hasn't changed.</param>
	private protected void PublishValue(object? value, BindingError? error = null, bool forceUpdate = false)
	{
		if (!IsRunning)
		{
			return;
		}
		if (TargetProperty == StyledElement.DataContextProperty && value == AvaloniaProperty.UnsetValue && error != null && error.ErrorType == BindingErrorType.Error)
		{
			value = null;
		}
		bool flag = forceUpdate || (value != UnchangedValue && !TypeUtilities.IdentityEquals(value, GetValue(), TargetType));
		bool flag2 = error != null || _error != null;
		if (flag)
		{
			_value = value;
		}
		_error = error;
		if (!_produceValue || _sink == null)
		{
			return;
		}
		if (Dispatcher.UIThread.CheckAccess())
		{
			_sink.OnChanged(this, flag, flag2, GetValueOrDefault(), _error);
			return;
		}
		IBindingExpressionSink sink = _sink;
		bool vc = flag;
		bool ec = flag2;
		object v = GetValueOrDefault();
		BindingError e = _error;
		Dispatcher.UIThread.Post(delegate
		{
			sink.OnChanged(this, vc, ec, v, e);
		});
	}

	/// <summary>
	/// Gets a value indicating whether an error should be logged given the current state of the
	/// binding expression.
	/// </summary>
	/// <param name="target">
	/// When the method returns, contains the target object, if it is available.
	/// </param>
	/// <returns>True if an error should be logged; otherwise false.</returns>
	protected virtual bool ShouldLogError([NotNullWhen(true)] out AvaloniaObject? target)
	{
		return TryGetTarget(out target);
	}

	/// <summary>
	/// Starts the binding expression by calling <see cref="M:Avalonia.Data.Core.UntypedBindingExpressionBase.StartCore" />.
	/// </summary>
	/// <param name="produceValue">
	/// Indicates whether the binding expression should produce an initial value.
	/// </param>
	protected void Start(bool produceValue)
	{
		if (_isRunning)
		{
			return;
		}
		_isRunning = true;
		try
		{
			_produceValue = produceValue;
			StartCore();
		}
		finally
		{
			_produceValue = true;
		}
	}

	/// <summary>
	/// When overridden in a derived class, starts the binding expression.
	/// </summary>
	/// <remarks>
	/// This method should not be called directly; instead call <see cref="M:Avalonia.Data.Core.UntypedBindingExpressionBase.Start(System.Boolean)" />.
	/// </remarks>
	protected abstract void StartCore();

	/// <summary>
	/// Stops the binding expression by calling <see cref="M:Avalonia.Data.Core.UntypedBindingExpressionBase.StopCore" />.
	/// </summary>
	protected void Stop()
	{
		if (_isRunning)
		{
			StopCore();
			_isRunning = false;
			_value = AvaloniaProperty.UnsetValue;
		}
	}

	/// <summary>
	/// When overridden in a derived class, stops the binding expression.
	/// </summary>
	/// <remarks>
	/// This method should not be called directly; instead call <see cref="M:Avalonia.Data.Core.UntypedBindingExpressionBase.Stop" />.
	/// </remarks>
	protected abstract void StopCore();

	/// <summary>
	/// Tries to retrieve the target for the binding expression.
	/// </summary>
	/// <param name="target">
	/// When this method returns, contains the target object, if it is available.
	/// </param>
	/// <returns>true if the target was retrieved; otherwise, false.</returns>
	protected bool TryGetTarget([NotNullWhen(true)] out AvaloniaObject? target)
	{
		if (_target != null)
		{
			return _target.TryGetTarget(out target);
		}
		target = null;
		return false;
	}

	private object? GetCachedDefaultValue()
	{
		if (_isDefaultValueInitialized)
		{
			return _defaultValue;
		}
		if ((object)TargetProperty != null)
		{
			WeakReference<AvaloniaObject?>? target = _target;
			if (target != null && target.TryGetTarget(out AvaloniaObject target2))
			{
				if (TargetProperty.IsDirect)
				{
					_defaultValue = ((IDirectPropertyAccessor)TargetProperty).GetUnsetValue(target2);
				}
				else
				{
					_defaultValue = ((IStyledPropertyAccessor)TargetProperty).GetDefaultValue(target2);
				}
				_isDefaultValueInitialized = true;
				return _defaultValue;
			}
		}
		return AvaloniaProperty.UnsetValue;
	}
}
