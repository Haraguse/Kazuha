using System;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Reads a property from a <see cref="T:Avalonia.AvaloniaObject" />.
/// </summary>
internal class AvaloniaPropertyAccessorPlugin : IPropertyAccessorPlugin
{
	private class Accessor : PropertyAccessorBase, IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
	{
		private readonly WeakReference<AvaloniaObject> _reference;

		private readonly AvaloniaProperty _property;

		public AvaloniaObject? Instance
		{
			get
			{
				_reference.TryGetTarget(out AvaloniaObject target);
				return target;
			}
		}

		public override Type? PropertyType => _property?.PropertyType;

		public override object? Value => Instance?.GetValue(_property);

		public Accessor(WeakReference<AvaloniaObject> reference, AvaloniaProperty property)
		{
			_reference = reference ?? throw new ArgumentNullException("reference");
			_property = property ?? throw new ArgumentNullException("property");
		}

		public override bool SetValue(object? value, BindingPriority priority)
		{
			if (!_property.IsReadOnly)
			{
				Instance?.SetValue(_property, value, priority);
				return true;
			}
			return false;
		}

		void IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>.OnEvent(object? notifyPropertyChanged, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Property == _property)
			{
				SendCurrentValue();
			}
		}

		protected override void SubscribeCore()
		{
			SubscribeToChanges();
			SendCurrentValue();
		}

		protected override void UnsubscribeCore()
		{
			AvaloniaObject instance = Instance;
			if (instance != null)
			{
				WeakEvents.AvaloniaPropertyChanged.Unsubscribe(instance, this);
			}
		}

		private void SendCurrentValue()
		{
			try
			{
				object value = Value;
				PublishValue(value);
			}
			catch
			{
			}
		}

		private void SubscribeToChanges()
		{
			AvaloniaObject instance = Instance;
			if (instance != null)
			{
				WeakEvents.AvaloniaPropertyChanged.Subscribe(instance, this);
			}
		}
	}

	/// <inheritdoc />
	public bool Match(object obj, string propertyName)
	{
		if (obj is AvaloniaObject o)
		{
			return LookupProperty(o, propertyName) != null;
		}
		return false;
	}

	/// <summary>
	/// Starts monitoring the value of a property on an object.
	/// </summary>
	/// <param name="reference">A weak reference to the object.</param>
	/// <param name="propertyName">The property name.</param>
	/// <returns>
	/// An <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> interface through which future interactions with the 
	/// property will be made.
	/// </returns>
	public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
	{
		if (reference == null)
		{
			throw new ArgumentNullException("reference");
		}
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		if (!reference.TryGetTarget(out object target) || target == null)
		{
			return null;
		}
		AvaloniaObject avaloniaObject = (AvaloniaObject)target;
		AvaloniaProperty avaloniaProperty = LookupProperty(avaloniaObject, propertyName);
		if (avaloniaProperty != null)
		{
			return new Accessor(new WeakReference<AvaloniaObject>(avaloniaObject), avaloniaProperty);
		}
		if (target != AvaloniaProperty.UnsetValue)
		{
			return new PropertyError(new BindingNotification(new MissingMemberException($"Could not find AvaloniaProperty '{propertyName}' on '{target}'"), BindingErrorType.Error));
		}
		return null;
	}

	private static AvaloniaProperty? LookupProperty(AvaloniaObject o, string propertyName)
	{
		return AvaloniaPropertyRegistry.Instance.FindRegistered(o, propertyName);
	}
}
