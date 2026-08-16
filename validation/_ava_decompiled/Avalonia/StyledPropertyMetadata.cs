using System;
using System.Runtime.CompilerServices;
using Avalonia.Data;

namespace Avalonia;

/// <summary>
/// Metadata for styled avalonia properties.
/// </summary>
public class StyledPropertyMetadata<TValue> : AvaloniaPropertyMetadata, IStyledPropertyMetadata
{
	private Optional<TValue> _defaultValue;

	/// <summary>
	/// Gets the default value for the property.
	/// </summary>
	public TValue DefaultValue
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _defaultValue.GetValueOrDefault();
		}
	}

	/// <summary>
	/// Gets the value coercion callback, if any.
	/// </summary>
	public Func<AvaloniaObject, TValue, TValue>? CoerceValue { get; private set; }

	object? IStyledPropertyMetadata.DefaultValue => DefaultValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.StyledPropertyMetadata`1" /> class.
	/// </summary>
	/// <param name="defaultValue">The default value of the property.</param>
	/// <param name="defaultBindingMode">The default binding mode.</param>
	/// <param name="coerce">A value coercion callback.</param>
	/// <param name="enableDataValidation">Whether the property is interested in data validation.</param>
	public StyledPropertyMetadata(Optional<TValue> defaultValue = default(Optional<TValue>), BindingMode defaultBindingMode = BindingMode.Default, Func<AvaloniaObject, TValue, TValue>? coerce = null, bool enableDataValidation = false)
		: base(defaultBindingMode, enableDataValidation)
	{
		_defaultValue = defaultValue;
		CoerceValue = coerce;
	}

	/// <inheritdoc />
	public override void Merge(AvaloniaPropertyMetadata baseMetadata, AvaloniaProperty property)
	{
		base.Merge(baseMetadata, property);
		if (baseMetadata is StyledPropertyMetadata<TValue> styledPropertyMetadata)
		{
			if (!_defaultValue.HasValue)
			{
				_defaultValue = styledPropertyMetadata.DefaultValue;
			}
			if (CoerceValue == null)
			{
				CoerceValue = styledPropertyMetadata.CoerceValue;
			}
		}
	}

	/// <inheritdoc />
	public override AvaloniaPropertyMetadata GenerateTypeSafeMetadata()
	{
		if (base.IsReadOnly && CoerceValue == null)
		{
			return this;
		}
		StyledPropertyMetadata<TValue> styledPropertyMetadata = new StyledPropertyMetadata<TValue>(DefaultValue, base.DefaultBindingMode, null, base.EnableDataValidation == true);
		styledPropertyMetadata.Freeze();
		return styledPropertyMetadata;
	}
}
