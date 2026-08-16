using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Metadata;

namespace Avalonia.Data;

/// <summary>
/// A binding which does not use reflection to access members.
/// </summary>
public class CompiledBinding : BindingBase
{
	/// <summary>
	/// Gets or sets the amount of time, in milliseconds, to wait before updating the binding 
	/// source after the value on the target changes.
	/// </summary>
	/// <remarks>
	/// There is no delay when the source is updated via <see cref="F:Avalonia.Data.UpdateSourceTrigger.LostFocus" /> 
	/// or <see cref="M:Avalonia.Data.BindingExpressionBase.UpdateSource" />. Nor is there a delay when 
	/// <see cref="F:Avalonia.Data.BindingMode.OneWayToSource" /> is active and a new source object is provided.
	/// </remarks>
	public int Delay { get; set; }

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
	/// Gets or sets a parameter to pass to <see cref="P:Avalonia.Data.CompiledBinding.Converter" />.
	/// </summary>
	public object? ConverterParameter { get; set; }

	/// <summary>
	/// Gets or sets the value to use when the binding is unable to produce a value.
	/// </summary>
	public object? FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets or sets the binding mode.
	/// </summary>
	public BindingMode Mode { get; set; }

	/// <summary>
	/// Gets or sets the binding path.
	/// </summary>
	[ConstructorArgument("path")]
	public CompiledBindingPath? Path { get; set; }

	/// <summary>
	/// Gets or sets the binding priority.
	/// </summary>
	public BindingPriority Priority { get; set; }

	/// <summary>
	/// Gets or sets the source for the binding.
	/// </summary>
	public object? Source { get; set; } = AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets or sets the string format.
	/// </summary>
	public string? StringFormat { get; set; }

	/// <summary>
	/// Gets or sets the value to use when the binding result is null.
	/// </summary>
	public object? TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets or sets a value that determines the timing of binding source updates for
	/// <see cref="F:Avalonia.Data.BindingMode.TwoWay" /> and <see cref="F:Avalonia.Data.BindingMode.OneWayToSource" /> bindings.
	/// </summary>
	public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

	internal WeakReference? DefaultAnchor { get; set; }

	internal WeakReference<INameScope?>? NameScope { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.CompiledBinding" /> class.
	/// </summary>
	public CompiledBinding()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.CompiledBinding" /> class.
	/// </summary>
	/// <param name="path">The binding path.</param>
	public CompiledBinding(CompiledBindingPath path)
	{
		Path = path;
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Data.CompiledBinding" /> from a lambda expression.
	/// </summary>
	/// <typeparam name="TIn">The input type of the binding expression.</typeparam>
	/// <typeparam name="TOut">The output type of the binding expression.</typeparam>
	/// <param name="expression">
	/// The lambda expression representing the binding path
	/// (e.g., <c>vm =&gt; vm.PropertyName</c>).
	/// </param>
	/// <param name="source">The source object for the binding. If null, uses the target's DataContext.
	/// </param>
	/// <param name="converter">
	/// Optional value converter to transform values between source and target.
	/// </param>
	/// <param name="mode">
	/// The binding mode. Default is <see cref="F:Avalonia.Data.BindingMode.Default" /> which resolves to the
	/// property's default binding mode.
	/// </param>
	/// <param name="priority">The binding priority.</param>
	/// <param name="converterCulture">The culture in which to evaluate the converter.</param>
	/// <param name="converterParameter">A parameter to pass to the converter.</param>
	/// <param name="fallbackValue">
	/// The value to use when the binding is unable to produce a value.
	/// </param>
	/// <param name="stringFormat">The string format for the binding result.</param>
	/// <param name="targetNullValue">The value to use when the binding result is null.</param>
	/// <param name="updateSourceTrigger">
	/// The timing of binding source updates for TwoWay/OneWayToSource bindings.
	/// </param>
	/// <param name="delay">
	/// The amount of time, in milliseconds, to wait before updating the binding source.
	/// </param>
	/// <returns>
	/// A configured <see cref="T:Avalonia.Data.CompiledBinding" /> instance ready to be applied to a property.
	/// </returns>
	/// <exception cref="T:Avalonia.Data.Core.ExpressionParseException">
	/// Thrown when the expression contains unsupported operations or invalid syntax for binding
	/// expressions.
	/// </exception>
	/// <remarks>
	/// This builds a <see cref="T:Avalonia.Data.CompiledBinding" /> with a path described by a lambda expression.
	/// The resulting binding avoids reflection for property access, providing better performance
	/// than reflection-based bindings.
	///
	/// Supported expressions include:
	/// <list type="bullet">
	/// <item>Property access: <c>x =&gt; x.Property</c></item>
	/// <item>Nested properties: <c>x =&gt; x.Property.Nested</c></item>
	/// <item>Indexers: <c>x =&gt; x.Items[0]</c></item>
	/// <item>Type casts: <c>x =&gt; ((DerivedType)x).Property</c></item>
	/// <item>Logical NOT: <c>x =&gt; !x.BoolProperty</c></item>
	/// <item>AvaloniaProperty access: <c>x =&gt; x[MyProperty]</c></item>
	/// </list>
	/// </remarks>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Expression statically preserves members used in binding expressions.")]
	public static CompiledBinding Create<TIn, TOut>(Expression<Func<TIn, TOut>> expression, object? source = null, IValueConverter? converter = null, BindingMode mode = BindingMode.Default, BindingPriority priority = BindingPriority.LocalValue, CultureInfo? converterCulture = null, object? converterParameter = null, object? fallbackValue = null, string? stringFormat = null, object? targetNullValue = null, UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default, int delay = 0)
	{
		return new CompiledBinding(BindingExpressionVisitor<TIn>.BuildPath(expression))
		{
			Source = (source ?? AvaloniaProperty.UnsetValue),
			Converter = converter,
			ConverterCulture = converterCulture,
			ConverterParameter = converterParameter,
			FallbackValue = (fallbackValue ?? AvaloniaProperty.UnsetValue),
			Mode = mode,
			Priority = priority,
			StringFormat = stringFormat,
			TargetNullValue = (targetNullValue ?? AvaloniaProperty.UnsetValue),
			UpdateSourceTrigger = updateSourceTrigger,
			Delay = delay
		};
	}

	internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
	{
		bool valueOrDefault = targetProperty?.GetMetadata(target).EnableDataValidation == true;
		List<ExpressionNode> list = new List<ExpressionNode>();
		bool isRooted = false;
		Path?.BuildExpression(list, out isRooted);
		if (Source == AvaloniaProperty.UnsetValue && !isRooted)
		{
			list.Insert(0, ExpressionNodeFactory.CreateDataContext(targetProperty));
		}
		object? source = ((list != null && list.Count > 0 && list[0] is SourceNode sourceNode) ? sourceNode.SelectSource(Source, target, anchor ?? DefaultAnchor?.Target) : ((Source != AvaloniaProperty.UnsetValue) ? Source : target));
		(BindingMode, UpdateSourceTrigger) tuple = ResolveDefaultsFromMetadata(target, targetProperty);
		return new BindingExpression(mode: tuple.Item1, updateSourceTrigger: tuple.Item2, source: source, nodes: list, fallbackValue: FallbackValue, delay: TimeSpan.FromMilliseconds(Delay), converter: Converter, converterCulture: ConverterCulture, converterParameter: ConverterParameter, enableDataValidation: valueOrDefault, priority: Priority, stringFormat: StringFormat, targetNullValue: TargetNullValue, targetProperty: targetProperty, targetTypeConverter: TargetTypeConverter.GetDefaultConverter());
	}

	private (BindingMode, UpdateSourceTrigger) ResolveDefaultsFromMetadata(AvaloniaObject target, AvaloniaProperty? targetProperty)
	{
		BindingMode bindingMode = Mode;
		UpdateSourceTrigger item = ((UpdateSourceTrigger == UpdateSourceTrigger.Default) ? UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger);
		if (bindingMode == BindingMode.Default)
		{
			bindingMode = (targetProperty?.GetMetadata(target))?.DefaultBindingMode ?? BindingMode.OneWay;
		}
		return (bindingMode, item);
	}
}
