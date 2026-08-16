using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

namespace Avalonia.Data;

/// <summary>
/// A XAML binding to a property on a control's templated parent.
/// </summary>
public sealed class TemplateBinding : BindingBase
{
	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Data.Converters.IValueConverter" /> to use.
	/// </summary>
	public IValueConverter? Converter { get; set; }

	/// <summary>
	/// Gets or sets the culture in which to evaluate the converter.
	/// </summary>
	/// <value>The default value is null.</value>
	/// <remarks>
	/// If this property is not set then <see cref="P:System.Globalization.CultureInfo.CurrentCulture" /> will be used.
	/// </remarks>
	[TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
	public CultureInfo? ConverterCulture { get; set; }

	/// <summary>
	/// Gets or sets a parameter to pass to <see cref="P:Avalonia.Data.TemplateBinding.Converter" />.
	/// </summary>
	public object? ConverterParameter { get; set; }

	/// <summary>
	/// Gets or sets the binding mode.
	/// </summary>
	public BindingMode Mode { get; set; }

	/// <summary>
	/// Gets or sets the name of the source property on the templated parent.
	/// </summary>
	[ConstructorArgument("property")]
	[InheritDataTypeFrom(InheritDataTypeFromScopeKind.ControlTemplate)]
	public AvaloniaProperty? Property { get; set; }

	public TemplateBinding()
	{
	}

	public TemplateBinding([InheritDataTypeFrom(InheritDataTypeFromScopeKind.ControlTemplate)] AvaloniaProperty property)
	{
		Property = property;
	}

	public BindingBase ProvideValue()
	{
		return this;
	}

	internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
	{
		BindingMode mode = Mode;
		if ((uint)(mode - 3) <= 1u)
		{
			throw new NotSupportedException("TemplateBinding does not support OneTime or OneWayToSource bindings.");
		}
		return new TemplateBindingExpression(Property, Converter, ConverterCulture, ConverterParameter, Mode);
	}
}
