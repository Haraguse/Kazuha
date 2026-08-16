using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class AvaloniaPropertyAccessorNode : ExpressionNode, ISettableNode, IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
{
	public AvaloniaProperty Property { get; }

	public Type? ValueType => Property.PropertyType;

	public AvaloniaPropertyAccessorNode(AvaloniaProperty property)
	{
		Property = property;
	}

	public override void BuildString(StringBuilder builder)
	{
		if (builder.Length > 0 && builder[builder.Length - 1] != '!')
		{
			builder.Append('.');
		}
		builder.Append(Property.Name);
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		if (base.Source is AvaloniaObject avaloniaObject)
		{
			avaloniaObject.SetValue(Property, value);
			return true;
		}
		return false;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source) && source is AvaloniaObject avaloniaObject)
		{
			WeakEvents.AvaloniaPropertyChanged.Subscribe(avaloniaObject, this);
			SetValue(avaloniaObject.GetValue(Property));
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		if (oldSource is AvaloniaObject target)
		{
			WeakEvents.AvaloniaPropertyChanged.Unsubscribe(target, this);
		}
	}

	public void OnEvent(object? sender, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == Property && base.Source is AvaloniaObject avaloniaObject)
		{
			SetValue(avaloniaObject.GetValue(Property));
		}
	}
}
