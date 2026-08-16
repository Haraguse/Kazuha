using System;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

internal class AvaloniaPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
{
	private readonly WeakReference<AvaloniaObject?> _reference;

	private readonly AvaloniaProperty _property;

	public AvaloniaObject? Instance
	{
		get
		{
			_reference.TryGetTarget(out AvaloniaObject target);
			return target;
		}
	}

	public override Type PropertyType => _property.PropertyType;

	public override object? Value => Instance?.GetValue(_property);

	public AvaloniaPropertyAccessor(WeakReference<AvaloniaObject?> reference, AvaloniaProperty property)
	{
		_reference = reference ?? throw new ArgumentNullException("reference");
		_property = property ?? throw new ArgumentNullException("property");
	}

	public override bool SetValue(object? value, BindingPriority priority)
	{
		if (!_property.IsReadOnly)
		{
			AvaloniaObject instance = Instance;
			if (instance != null)
			{
				instance.SetValue(_property, value, priority);
				return true;
			}
		}
		return false;
	}

	public void OnEvent(object? sender, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			PublishValue(Value);
		}
	}

	protected override void SubscribeCore()
	{
		if (_reference.TryGetTarget(out AvaloniaObject target))
		{
			object value = target.GetValue(_property);
			PublishValue(value);
			WeakEvents.AvaloniaPropertyChanged.Subscribe(target, this);
		}
	}

	protected override void UnsubscribeCore()
	{
		if (_reference.TryGetTarget(out AvaloniaObject target))
		{
			WeakEvents.AvaloniaPropertyChanged.Unsubscribe(target, this);
		}
	}
}
