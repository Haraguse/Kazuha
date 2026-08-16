using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Styling.Activators;
using Avalonia.Utilities;

namespace Avalonia.Styling;

/// <summary>
/// A selector that matches the common case of a type and/or name followed by a collection of
/// style classes and pseudoclasses.
/// </summary>
internal class PropertyEqualsSelector : Selector
{
	private readonly Selector? _previous;

	private readonly AvaloniaProperty _property;

	private readonly object? _value;

	private string? _selectorString;

	/// <inheritdoc />
	internal override bool InTemplate => _previous?.InTemplate ?? false;

	/// <inheritdoc />
	internal override bool IsCombinator => false;

	/// <inheritdoc />
	internal override Type? TargetType => _previous?.TargetType;

	public PropertyEqualsSelector(Selector? previous, AvaloniaProperty property, object? value)
	{
		property = property ?? throw new ArgumentNullException("property");
		_previous = previous;
		_property = property;
		_value = value;
	}

	/// <inheritdoc />
	public override string ToString(Style? owner)
	{
		if (_selectorString == null)
		{
			StringBuilder stringBuilder = StringBuilderCache.Acquire();
			if (_previous != null)
			{
				stringBuilder.Append(_previous.ToString(owner, hasNext: true));
			}
			stringBuilder.Append('[');
			if (_property.IsAttached)
			{
				stringBuilder.Append('(');
				stringBuilder.Append(_property.OwnerType.Name);
				stringBuilder.Append('.');
			}
			stringBuilder.Append(_property.Name);
			if (_property.IsAttached)
			{
				stringBuilder.Append(')');
			}
			stringBuilder.Append('=');
			stringBuilder.Append(_value ?? string.Empty);
			stringBuilder.Append(']');
			_selectorString = StringBuilderCache.GetStringAndRelease(stringBuilder);
		}
		return _selectorString;
	}

	/// <inheritdoc />
	private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
	{
		if (subscribe)
		{
			return new SelectorMatch(new PropertyEqualsActivator(control, _property, _value));
		}
		if (!Compare(_property.PropertyType, control.GetValue(_property), _value))
		{
			return SelectorMatch.NeverThisInstance;
		}
		return SelectorMatch.AlwaysThisInstance;
	}

	private protected override Selector? MovePrevious()
	{
		return _previous;
	}

	private protected override Selector? MovePreviousOrParent()
	{
		return _previous;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
	internal static bool Compare(Type propertyType, object? propertyValue, object? value)
	{
		if (propertyType == typeof(object))
		{
			Type type = propertyValue?.GetType();
			if ((object)type != null)
			{
				propertyType = type;
			}
		}
		Type type2 = value?.GetType();
		if ((object)type2 == null || propertyType.IsAssignableFrom(type2))
		{
			return object.Equals(propertyValue, value);
		}
		TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
		if (converter != null && converter.CanConvertFrom(type2))
		{
			return object.Equals(propertyValue, converter.ConvertFrom(null, CultureInfo.InvariantCulture, value));
		}
		return false;
	}
}
