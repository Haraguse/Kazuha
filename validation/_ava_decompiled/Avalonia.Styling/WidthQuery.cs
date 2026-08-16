using System;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

internal sealed class WidthQuery : ValueStyleQuery<(StyleQueryComparisonOperator @operator, double value)>
{
	public WidthQuery(StyleQuery? previous, StyleQueryComparisonOperator @operator, double value)
		: base(previous, (@operator, value))
	{
	}

	internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
	{
		if (!(control is Visual visual))
		{
			return SelectorMatch.NeverThisType;
		}
		if (subscribe)
		{
			return new SelectorMatch(new WidthActivator(visual, base.Argument, containerName));
		}
		Layoutable container = ContainerQueryActivatorBase.GetContainer(visual, containerName);
		if (container != null && container != null)
		{
			Layoutable layoutable = container;
			VisualQueryProvider queryProvider = Container.GetQueryProvider(layoutable);
			if (queryProvider != null && Container.GetSizing(layoutable) == ContainerSizing.WidthAndHeight)
			{
				return Evaluate(queryProvider, base.Argument);
			}
		}
		return SelectorMatch.NeverThisInstance;
	}

	internal static SelectorMatch Evaluate(VisualQueryProvider queryProvider, (StyleQueryComparisonOperator @operator, double value) argument)
	{
		double width = queryProvider.Width;
		if (double.IsNaN(width))
		{
			return SelectorMatch.NeverThisInstance;
		}
		if (!IsTrue(argument.@operator, argument.value))
		{
			return SelectorMatch.NeverThisInstance;
		}
		return SelectorMatch.AlwaysThisInstance;
		bool IsTrue(StyleQueryComparisonOperator comparisonOperator, double value)
		{
			return comparisonOperator switch
			{
				StyleQueryComparisonOperator.None => true, 
				StyleQueryComparisonOperator.Equals => width == value, 
				StyleQueryComparisonOperator.LessThan => width < value, 
				StyleQueryComparisonOperator.GreaterThan => width > value, 
				StyleQueryComparisonOperator.LessThanOrEquals => width <= value, 
				StyleQueryComparisonOperator.GreaterThanOrEquals => width >= value, 
				_ => false, 
			};
		}
	}

	public override string ToString()
	{
		return ToString(null);
	}

	public override string ToString(ContainerQuery? owner)
	{
		string value = base.Argument.@operator switch
		{
			StyleQueryComparisonOperator.None => "", 
			StyleQueryComparisonOperator.Equals => "width", 
			StyleQueryComparisonOperator.LessThan => "", 
			StyleQueryComparisonOperator.GreaterThan => "", 
			StyleQueryComparisonOperator.LessThanOrEquals => "max-width", 
			StyleQueryComparisonOperator.GreaterThanOrEquals => "min-width", 
			_ => throw new NotImplementedException(), 
		};
		return $"{value}:{base.Argument.value}";
	}
}
