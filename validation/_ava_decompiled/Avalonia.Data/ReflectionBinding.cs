using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Data;

/// <summary>
/// A binding that uses reflection to access members.
/// </summary>
[RequiresUnreferencedCode("BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.")]
[RequiresDynamicCode("BindingExpression and ReflectionBinding require dynamic code. Consider using CompiledBindings instead.")]
public class ReflectionBinding : BindingBase
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
	/// Gets or sets a parameter to pass to <see cref="P:Avalonia.Data.ReflectionBinding.Converter" />.
	/// </summary>
	public object? ConverterParameter { get; set; }

	/// <summary>
	/// Gets or sets the name of the element to use as the binding source.
	/// </summary>
	public string? ElementName { get; set; }

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
	public string Path { get; set; } = "";

	/// <summary>
	/// Gets or sets the binding priority.
	/// </summary>
	public BindingPriority Priority { get; set; }

	/// <summary>
	/// Gets or sets the relative source for the binding.
	/// </summary>
	public RelativeSource? RelativeSource { get; set; }

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

	/// <summary>
	/// Gets or sets a function used to resolve types from names in the binding path.
	/// </summary>
	public Func<string?, string, Type>? TypeResolver { get; set; }

	internal WeakReference? DefaultAnchor { get; set; }

	internal WeakReference<INameScope?>? NameScope { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.ReflectionBinding" /> class.
	/// </summary>
	public ReflectionBinding()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.ReflectionBinding" /> class.
	/// </summary>
	/// <param name="path">The binding path.</param>
	public ReflectionBinding(string path)
	{
		Path = path;
	}

	internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
	{
		List<ExpressionNode> list = null;
		bool isRooted = false;
		bool valueOrDefault = targetProperty?.GetMetadata(target).EnableDataValidation == true;
		if (!string.IsNullOrEmpty(Path))
		{
			CharacterReader r = new CharacterReader(Path.AsSpan());
			List<BindingExpressionGrammar.INode> item = BindingExpressionGrammar.ParseToPooledList(ref r).Nodes;
			list = ExpressionNodeFactory.CreateFromAst(item, TypeResolver, GetNameScope(), out isRooted);
		}
		if (Source == AvaloniaProperty.UnsetValue && !isRooted)
		{
			ExpressionNode expressionNode = CreateSourceNode(targetProperty);
			if (expressionNode != null)
			{
				if (list == null)
				{
					list = new List<ExpressionNode>();
				}
				list.Insert(0, expressionNode);
			}
		}
		object? source = ((list != null && list.Count > 0 && list[0] is SourceNode sourceNode) ? sourceNode.SelectSource(Source, target, anchor ?? DefaultAnchor?.Target) : ((Source != AvaloniaProperty.UnsetValue) ? Source : target));
		(BindingMode, UpdateSourceTrigger) tuple = ResolveDefaultsFromMetadata(target, targetProperty);
		BindingMode item2 = tuple.Item1;
		UpdateSourceTrigger item3 = tuple.Item2;
		return new BindingExpression(source, list, FallbackValue, TimeSpan.FromMilliseconds(Delay), Converter, ConverterCulture, ConverterParameter, valueOrDefault, item2, Priority, StringFormat, TargetNullValue, targetProperty, TargetTypeConverter.GetReflectionConverter(), item3);
	}

	private INameScope? GetNameScope()
	{
		INameScope target = null;
		NameScope?.TryGetTarget(out target);
		return target;
	}

	private ExpressionNode? CreateSourceNode(AvaloniaProperty? targetProperty)
	{
		if (!string.IsNullOrEmpty(ElementName))
		{
			return new NamedElementNode(GetNameScope() ?? throw new InvalidOperationException("Cannot create ElementName binding when NameScope is null"), ElementName);
		}
		if (RelativeSource != null)
		{
			return ExpressionNodeFactory.CreateRelativeSource(RelativeSource);
		}
		return ExpressionNodeFactory.CreateDataContext(targetProperty);
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
