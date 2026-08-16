using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.PropertyStore;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// An object with <see cref="T:Avalonia.AvaloniaProperty" /> support.
/// </summary>
/// <remarks>
/// This class is analogous to DependencyObject in WPF.
/// </remarks>
[DebuggerDisplay("{DebugDisplay}")]
public class AvaloniaObject : IAvaloniaObjectDebug, INotifyPropertyChanged
{
	private readonly ValueStore _values;

	private AvaloniaObject? _inheritanceParent;

	private PropertyChangedEventHandler? _inpcChanged;

	private EventHandler<AvaloniaPropertyChangedEventArgs>? _propertyChanged;

	private List<AvaloniaObject>? _inheritanceChildren;

	/// <summary>
	/// Gets or sets the parent object that inherited <see cref="T:Avalonia.AvaloniaProperty" /> values
	/// are inherited from.
	/// </summary>
	/// <value>
	/// The inheritance parent.
	/// </value>
	protected internal AvaloniaObject? InheritanceParent
	{
		get
		{
			return _inheritanceParent;
		}
		set
		{
			VerifyAccess();
			if (_inheritanceParent != value)
			{
				_inheritanceParent?.RemoveInheritanceChild(this);
				_inheritanceParent = value;
				_inheritanceParent?.AddInheritanceChild(this);
				_values.SetInheritanceParent(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets the value of a <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="property">The property.</param>
	public object? this[AvaloniaProperty property]
	{
		get
		{
			return GetValue(property);
		}
		set
		{
			SetValue(property, value);
		}
	}

	/// <summary>
	/// Gets or sets a binding for a <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="binding">The binding information.</param>
	public BindingBase this[IndexerDescriptor binding]
	{
		get
		{
			return new IndexerBinding(this, binding.Property, binding.Mode);
		}
		set
		{
			Bind(binding.Property, value);
		}
	}

	/// <summary>
	/// Gets a string to display inside the debugger for this object.
	/// </summary>
	internal string DebugDisplay => GetDebugDisplay(includeContent: true);

	/// <summary>
	///     Returns the <see cref="P:Avalonia.AvaloniaObject.Dispatcher" /> that this
	///     <see cref="T:Avalonia.AvaloniaObject" /> is associated with.
	/// </summary>
	public Dispatcher Dispatcher { get; } = Avalonia.Threading.Dispatcher.CurrentDispatcher;

	/// <summary>
	/// Raised when a <see cref="T:Avalonia.AvaloniaProperty" /> value changes on this object.
	/// </summary>
	public event EventHandler<AvaloniaPropertyChangedEventArgs>? PropertyChanged
	{
		add
		{
			_propertyChanged = (EventHandler<AvaloniaPropertyChangedEventArgs>)Delegate.Combine(_propertyChanged, value);
		}
		remove
		{
			_propertyChanged = (EventHandler<AvaloniaPropertyChangedEventArgs>)Delegate.Remove(_propertyChanged, value);
		}
	}

	/// <summary>
	/// Raised when a <see cref="T:Avalonia.AvaloniaProperty" /> value changes on this object.
	/// </summary>
	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add
		{
			_inpcChanged = (PropertyChangedEventHandler)Delegate.Combine(_inpcChanged, value);
		}
		remove
		{
			_inpcChanged = (PropertyChangedEventHandler)Delegate.Remove(_inpcChanged, value);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.AvaloniaObject" /> class.
	/// </summary>
	public AvaloniaObject()
	{
		_values = new ValueStore(this);
	}

	/// <summary>
	/// Returns a value indicating whether the current thread is the UI thread.
	/// </summary>
	/// <returns>true if the current thread is the UI thread; otherwise false.</returns>
	public bool CheckAccess()
	{
		return Dispatcher.CheckAccess();
	}

	/// <summary>
	/// Checks that the current thread is the UI thread and throws if not.
	/// </summary>
	public void VerifyAccess()
	{
		Dispatcher.VerifyAccess();
	}

	/// <summary>
	/// Clears a <see cref="T:Avalonia.AvaloniaProperty" />'s local value.
	/// </summary>
	/// <param name="property">The property.</param>
	public void ClearValue(AvaloniaProperty property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		_values.ClearValue(property);
	}

	/// <summary>
	/// Clears a <see cref="T:Avalonia.AvaloniaProperty" />'s local value.
	/// </summary>
	/// <param name="property">The property.</param>
	public void ClearValue<T>(AvaloniaProperty<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		if (!(property is StyledProperty<T> property2))
		{
			if (!(property is DirectPropertyBase<T> property3))
			{
				throw new NotSupportedException("Unsupported AvaloniaProperty type.");
			}
			ClearValue(property3);
		}
		else
		{
			ClearValue(property2);
		}
	}

	/// <summary>
	/// Clears a <see cref="T:Avalonia.AvaloniaProperty" />'s local value.
	/// </summary>
	/// <param name="property">The property.</param>
	public void ClearValue<T>(StyledProperty<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		_values.ClearValue(property);
	}

	/// <summary>
	/// Clears a <see cref="T:Avalonia.AvaloniaProperty" />'s local value.
	/// </summary>
	/// <param name="property">The property.</param>
	public void ClearValue<T>(DirectPropertyBase<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		DirectPropertyBase<T> registeredDirect = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
		registeredDirect.InvokeSetter(this, registeredDirect.GetUnsetValue(this));
	}

	/// <summary>
	/// Compares two objects using reference equality.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <remarks>
	/// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
	///
	/// - AvaloniaObjects are by their nature mutable
	/// - The presence of attached properties means that the semantics of equality are
	///   difficult to define
	///
	/// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
	/// this.
	/// </remarks>
	public sealed override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	/// <summary>
	/// Gets the hash code for the object.
	/// </summary>
	/// <remarks>
	/// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
	///
	/// - AvaloniaObjects are by their nature mutable
	/// - The presence of attached properties means that the semantics of equality are
	///   difficult to define
	///
	/// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
	/// this.
	/// </remarks>
	public sealed override int GetHashCode()
	{
		return base.GetHashCode();
	}

	/// <summary>
	/// Gets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>The value.</returns>
	public object? GetValue(AvaloniaProperty property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		if (property.IsDirect)
		{
			return property.RouteGetValue(this);
		}
		return _values.GetValue(property);
	}

	/// <summary>
	/// Gets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>The value.</returns>
	public T GetValue<T>(StyledProperty<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		return _values.GetValue(property);
	}

	/// <summary>
	/// Gets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>The value.</returns>
	public T GetValue<T>(DirectPropertyBase<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		return AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property).InvokeGetter(this);
	}

	/// <summary>
	/// Gets an <see cref="T:Avalonia.AvaloniaProperty" /> base value.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <remarks>
	/// Gets the value of the property excluding animated values, otherwise <see cref="P:Avalonia.Data.Optional`1.Empty" />.
	/// Note that this method does not return property values that come from inherited or default values.
	/// </remarks>
	public Optional<T> GetBaseValue<T>(StyledProperty<T> property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		return _values.GetBaseValue(property);
	}

	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.AvaloniaProperty" /> is animating.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>True if the property is animating, otherwise false.</returns>
	public bool IsAnimating(AvaloniaProperty property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		return _values.IsAnimating(property);
	}

	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.AvaloniaProperty" /> is set on this object.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>True if the property is set, otherwise false.</returns>
	/// <remarks>
	/// Returns true if <paramref name="property" /> is a styled property which has a value
	/// assigned to it or a binding targeting it; otherwise false.
	/// </remarks>
	public bool IsSet(AvaloniaProperty property)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		return _values.IsSet(property);
	}

	/// <summary>
	/// Sets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	/// <param name="priority">The priority of the value.</param>
	public IDisposable? SetValue(AvaloniaProperty property, object? value, BindingPriority priority = BindingPriority.LocalValue)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		return property.RouteSetValue(this, value, priority);
	}

	/// <summary>
	/// Sets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	/// <param name="priority">The priority of the value.</param>
	/// <returns>
	/// An <see cref="T:System.IDisposable" /> if setting the property can be undone, otherwise null.
	/// </returns>
	public IDisposable? SetValue<T>(StyledProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		ValidatePriority(priority);
		LogPropertySet(property, value, priority);
		if (value is UnsetValueType)
		{
			if (priority == BindingPriority.LocalValue)
			{
				_values.ClearValue(property);
			}
		}
		else if (!(value is DoNothingType))
		{
			return _values.SetValue(property, value, priority);
		}
		return null;
	}

	/// <summary>
	/// Sets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	public void SetValue<T>(DirectPropertyBase<T> property, T value)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
		LogPropertySet(property, value, BindingPriority.LocalValue);
		SetDirectValueUnchecked(property, value);
	}

	/// <summary>
	/// Sets the value of a dependency property without changing its value source.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	/// <remarks>
	/// This method is used by a component that programmatically sets the value of one of its
	/// own properties without disabling an application's declared use of the property. The
	/// method changes the effective value of the property, but existing data bindings and
	/// styles will continue to work.
	///
	/// The new value will have the property's current <see cref="T:Avalonia.Data.BindingPriority" />, even if
	/// that priority is <see cref="F:Avalonia.Data.BindingPriority.Unset" /> or 
	/// <see cref="F:Avalonia.Data.BindingPriority.Inherited" />.
	/// </remarks>
	public void SetCurrentValue(AvaloniaProperty property, object? value)
	{
		property.RouteSetCurrentValue(this, value);
	}

	/// <summary>
	/// Sets the value of a dependency property without changing its value source.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	/// <remarks>
	/// This method is used by a component that programmatically sets the value of one of its
	/// own properties without disabling an application's declared use of the property. The
	/// method changes the effective value of the property, but existing data bindings and
	/// styles will continue to work.
	///
	/// The new value will have the property's current <see cref="T:Avalonia.Data.BindingPriority" />, even if
	/// that priority is <see cref="F:Avalonia.Data.BindingPriority.Unset" /> or 
	/// <see cref="F:Avalonia.Data.BindingPriority.Inherited" />.
	/// </remarks>
	public void SetCurrentValue<T>(StyledProperty<T> property, T value)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		LogPropertySet(property, value, BindingPriority.LocalValue);
		if (value is UnsetValueType)
		{
			_values.ClearValue(property);
		}
		else if (!(value is DoNothingType))
		{
			_values.SetCurrentValue(property, value);
		}
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an <see cref="T:Avalonia.Data.BindingBase" />.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="binding">The binding.</param>
	/// <returns>
	/// The binding expression which represents the binding instance on this object.
	/// </returns>
	public BindingExpressionBase Bind(AvaloniaProperty property, BindingBase binding)
	{
		return Bind(property, binding, null);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind(AvaloniaProperty property, IObservable<object?> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		return property.RouteBind(this, source, priority);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(StyledProperty<T> property, IObservable<object?> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		ThrowHelper.ThrowIfNull(source, "source");
		VerifyAccess();
		ValidatePriority(priority);
		return _values.AddBinding(property, source, priority);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(StyledProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		ThrowHelper.ThrowIfNull(source, "source");
		VerifyAccess();
		ValidatePriority(priority);
		return _values.AddBinding(property, source, priority);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(StyledProperty<T> property, IObservable<BindingValue<T>> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		ThrowHelper.ThrowIfNull(source, "source");
		VerifyAccess();
		ValidatePriority(priority);
		return _values.AddBinding(property, source, priority);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(DirectPropertyBase<T> property, IObservable<object?> source)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
		ThrowIfReadOnly(property);
		return _values.AddBinding(property, source);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(DirectPropertyBase<T> property, IObservable<T> source)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
		ThrowIfReadOnly(property);
		return _values.AddBinding(property, source);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public IDisposable Bind<T>(DirectPropertyBase<T> property, IObservable<BindingValue<T>> source)
	{
		ThrowHelper.ThrowIfNull(property, "property");
		VerifyAccess();
		property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
		ThrowIfReadOnly(property);
		return _values.AddBinding(property, source);
	}

	/// <summary>
	/// Coerces the specified <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="property">The property.</param>
	public void CoerceValue(AvaloniaProperty property)
	{
		_values.CoerceValue(property);
	}

	/// <summary>
	/// Binds a <see cref="T:Avalonia.AvaloniaProperty" /> to an <see cref="T:Avalonia.Data.BindingBase" />.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="binding">The binding.</param>
	/// <param name="anchor">
	/// An optional anchor from which to locate required context. When binding to objects that
	/// are not in the logical tree, certain types of binding need an anchor into the tree in 
	/// order to locate named controls or resources. The <paramref name="anchor" /> parameter 
	/// can be used to provide this context.
	/// </param>
	/// <returns>
	/// The binding expression which represents the binding instance on this object.
	/// </returns>
	internal BindingExpressionBase Bind(AvaloniaProperty property, BindingBase binding, object? anchor)
	{
		if (!(binding.CreateInstance(this, property, anchor) is UntypedBindingExpressionBase source))
		{
			throw new NotSupportedException("Binding returned unsupported BindingExpressionBase.");
		}
		return GetValueStore().AddBinding(property, source);
	}

	internal void AddInheritanceChild(AvaloniaObject child)
	{
		if (_inheritanceChildren == null)
		{
			_inheritanceChildren = new List<AvaloniaObject>();
		}
		_inheritanceChildren.Add(child);
	}

	internal void RemoveInheritanceChild(AvaloniaObject child)
	{
		_inheritanceChildren?.Remove(child);
	}

	/// <inheritdoc />
	Delegate[]? IAvaloniaObjectDebug.GetPropertyChangedSubscribers()
	{
		return _propertyChanged?.GetInvocationList();
	}

	internal AvaloniaPropertyValue GetDiagnosticInternal(AvaloniaProperty property)
	{
		if (property.IsDirect)
		{
			return new AvaloniaPropertyValue(property, GetValue(property), BindingPriority.LocalValue, null, isOverriddenCurrentValue: false);
		}
		return _values.GetDiagnostic(property);
	}

	internal ValueStore GetValueStore()
	{
		return _values;
	}

	internal IReadOnlyList<AvaloniaObject>? GetInheritanceChildren()
	{
		return _inheritanceChildren;
	}

	/// <summary>
	/// Called to update the validation state for properties for which data validation is
	/// enabled.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="state">The current data binding state.</param>
	/// <param name="error">The current data binding error, if any.</param>
	protected virtual void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
	{
	}

	/// <summary>
	/// Called when a avalonia property changes on the object.
	/// </summary>
	/// <param name="change">The property change details.</param>
	protected virtual void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.IsEffectiveValueChange)
		{
			OnPropertyChanged(change);
		}
	}

	/// <summary>
	/// Called when a avalonia property changes on the object.
	/// </summary>
	/// <param name="change">The property change details.</param>
	protected virtual void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> event for a direct property.
	/// </summary>
	/// <param name="property">The property that has changed.</param>
	/// <param name="oldValue">The old property value.</param>
	/// <param name="newValue">The new property value.</param>
	protected void RaisePropertyChanged<T>(DirectPropertyBase<T> property, T oldValue, T newValue)
	{
		RaisePropertyChanged(property, oldValue, newValue, BindingPriority.LocalValue, isEffectiveValue: true);
	}

	/// <summary>
	/// This is an optimized path for <see cref="M:Avalonia.AvaloniaObject.RaisePropertyChanged``1(Avalonia.DirectPropertyBase{``0},``0,``0)" />.
	/// This will reuse the event args in situations where many allocations would otherwise happen.
	/// </summary>
	/// <param name="args">Avalonia property change args</param>
	/// <param name="inpcArgs">INPC event args/</param>
	internal void RaisePropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> args, PropertyChangedEventArgs? inpcArgs)
	{
		OnPropertyChangedCore(args);
		if (args.IsEffectiveValueChange && inpcArgs != null)
		{
			args.Property.NotifyChanged(args);
			_propertyChanged?.Invoke(this, args);
			_inpcChanged?.Invoke(this, inpcArgs);
		}
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> event.
	/// </summary>
	/// <param name="property">The property that has changed.</param>
	/// <param name="oldValue">The old property value.</param>
	/// <param name="newValue">The new property value.</param>
	/// <param name="priority">The priority of the binding that produced the value.</param>
	/// <param name="isEffectiveValue">
	/// Whether the notification represents a change to the effective value of the property.
	/// </param>
	internal void RaisePropertyChanged<T>(AvaloniaProperty<T> property, Optional<T> oldValue, BindingValue<T> newValue, BindingPriority priority, bool isEffectiveValue)
	{
		AvaloniaPropertyChangedEventArgs<T> e = new AvaloniaPropertyChangedEventArgs<T>(this, property, oldValue, newValue, priority, isEffectiveValue);
		OnPropertyChangedCore(e);
		if (isEffectiveValue)
		{
			property.NotifyChanged(e);
			_propertyChanged?.Invoke(this, e);
			_inpcChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
		}
	}

	/// <summary>
	/// Sets the backing field for a direct avalonia property, raising the 
	/// <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> event if the value has changed.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <param name="field">The backing field.</param>
	/// <param name="value">The value.</param>
	/// <returns>
	/// True if the value changed, otherwise false.
	/// </returns>
	protected bool SetAndRaise<T>(DirectPropertyBase<T> property, ref T field, T value)
	{
		VerifyAccess();
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		T val = field;
		field = value;
		RaisePropertyChanged(property, val, value, BindingPriority.LocalValue, isEffectiveValue: true);
		return true;
	}

	/// <summary>
	/// Sets the value of a direct property.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, T value)
	{
		if (value is UnsetValueType)
		{
			property.InvokeSetter(this, property.GetUnsetValue(this));
		}
		else if (!(value is DoNothingType))
		{
			property.InvokeSetter(this, value);
		}
	}

	/// <summary>
	/// Sets the value of a direct property.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="value">The value.</param>
	internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, BindingValue<T> value)
	{
		switch (value.Type)
		{
		case BindingValueType.UnsetValue:
		case BindingValueType.BindingError:
		{
			BindingValue<T> value2 = (value.HasValue ? value : value.WithValue(property.GetUnsetValue(this)));
			property.InvokeSetter(this, value2);
			break;
		}
		case BindingValueType.Value:
		case BindingValueType.DataValidationError:
		case BindingValueType.BindingErrorWithFallback:
		case BindingValueType.DataValidationErrorWithFallback:
			property.InvokeSetter(this, value);
			break;
		}
		if (property.GetMetadata(this).EnableDataValidation == true)
		{
			UpdateDataValidation(property, value.Type, value.Error);
		}
	}

	internal void OnUpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
	{
		UpdateDataValidation(property, state, error);
	}

	/// <summary>
	/// Gets a description of an observable that can be used in logs.
	/// </summary>
	/// <param name="o">The observable.</param>
	/// <returns>The description.</returns>
	private string GetDescription(object o)
	{
		return (o as IDescription)?.Description ?? o.ToString() ?? o.GetType().Name;
	}

	/// <summary>
	/// Logs a property set message.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="value">The new value.</param>
	/// <param name="priority">The priority.</param>
	private void LogPropertySet<T>(AvaloniaProperty<T> property, T value, BindingPriority priority)
	{
		Logger.TryGet(LogEventLevel.Verbose, "Property")?.Log(this, "Set {Property} to {$Value} with priority {Priority}", property, value, priority);
	}

	internal string GetDebugDisplay(bool includeContent)
	{
		StringBuilder stringBuilder = new StringBuilder();
		BuildDebugDisplay(stringBuilder, includeContent);
		return stringBuilder.ToString();
	}

	internal virtual void BuildDebugDisplay(StringBuilder builder, bool includeContent)
	{
		Type type = GetType();
		string text = type.Namespace;
		if (text != null && (text == "Avalonia" || text.StartsWith("Avalonia.", StringComparison.Ordinal)))
		{
			builder.Append(type.Name);
		}
		else
		{
			builder.Append(ToString());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ValidatePriority(BindingPriority priority)
	{
		if (priority < BindingPriority.Animation || priority >= BindingPriority.Inherited)
		{
			ThrowInvalidPriority(priority);
		}
	}

	private static void ThrowIfReadOnly(AvaloniaProperty property)
	{
		if (property.IsReadOnly)
		{
			throw new ArgumentException("The property " + property.Name + " is readonly.");
		}
	}

	private static void ThrowInvalidPriority(BindingPriority priority)
	{
		throw new ArgumentException($"Invalid priority ${priority}", "priority");
	}
}
