using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode("ExpressionNode might require unreferenced code.")]
internal sealed class ExpressionTreeIndexerNode : CollectionNodeBase, ISettableNode
{
	private readonly ParameterExpression _parameter;

	private readonly IndexExpression _expression;

	private readonly Delegate _setDelegate;

	private readonly Delegate _getDelegate;

	private readonly Delegate _firstArgumentDelegate;

	public Type? ValueType => _expression.Type;

	[RequiresDynamicCode("ExpressionNode requires dynamic code.")]
	public ExpressionTreeIndexerNode(IndexExpression expression)
	{
		ParameterExpression parameterExpression = Expression.Parameter(expression.Type);
		_parameter = Expression.Parameter(expression.Object.Type);
		_expression = expression.Update(_parameter, expression.Arguments);
		_getDelegate = Expression.Lambda(_expression, _parameter).Compile();
		_setDelegate = Expression.Lambda(Expression.Assign(_expression, parameterExpression), _parameter, parameterExpression).Compile();
		_firstArgumentDelegate = Expression.Lambda(_expression.Arguments[0], _parameter).Compile();
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('[');
		for (int i = 1; i < _expression.Arguments.Count; i++)
		{
			if (i > 1)
			{
				builder.Append(", ");
			}
			builder.Append(_expression.Arguments[i]);
		}
		builder.Append(_expression.Arguments[0]);
		builder.Append(']');
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		if (base.Source == null)
		{
			return false;
		}
		_setDelegate.DynamicInvoke(base.Source, value);
		return true;
	}

	protected override bool ShouldUpdate(object? sender, PropertyChangedEventArgs e)
	{
		if (!(_expression.Indexer == null))
		{
			return _expression.Indexer.Name == e.PropertyName;
		}
		return true;
	}

	protected override int? TryGetFirstArgumentAsInt()
	{
		object source = base.Source;
		if (source == null)
		{
			return null;
		}
		return _firstArgumentDelegate.DynamicInvoke(source) as int?;
	}

	protected override void UpdateValue(object? source)
	{
		try
		{
			if (source != null)
			{
				SetValue(_getDelegate.DynamicInvoke(source));
			}
			else
			{
				SetValue(null);
			}
		}
		catch (Exception error)
		{
			SetError(error);
		}
	}
}
