using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="T:Avalonia.Data.Core.BindingExpression" /> which accesses an array with integer
/// indexers.
/// </summary>
internal sealed class ArrayIndexerNode : ExpressionNode, ISettableNode
{
	private readonly int[] _indexes;

	public Type? ValueType => base.Source?.GetType().GetElementType();

	public ArrayIndexerNode(int[] indexes)
	{
		_indexes = indexes;
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('[');
		for (int i = 0; i < _indexes.Length; i++)
		{
			builder.Append(_indexes[i]);
			if (i != _indexes.Length - 1)
			{
				builder.Append(',');
			}
		}
		builder.Append(']');
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		if (base.Source is Array array)
		{
			array.SetValue(value, _indexes);
			return true;
		}
		return false;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (source is Array array)
			{
				SetValue(array.GetValue(_indexes));
			}
			else
			{
				ClearValue();
			}
		}
	}
}
