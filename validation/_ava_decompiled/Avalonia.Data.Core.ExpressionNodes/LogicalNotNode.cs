using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class LogicalNotNode : ExpressionNode, ISettableNode
{
	public Type ValueType => typeof(bool);

	public override void BuildString(StringBuilder builder)
	{
		builder.Append("!");
	}

	public override void BuildString(StringBuilder builder, IReadOnlyList<ExpressionNode> nodes)
	{
		builder.Append("!");
		if (base.Index > 0)
		{
			nodes[base.Index - 1].BuildString(builder, nodes);
		}
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		if (base.Index > 0 && nodes[base.Index - 1] is ISettableNode settableNode && TryConvert(value, out var result))
		{
			return settableNode.WriteValueToSource(!result, nodes);
		}
		return false;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (TryConvert(BindingNotification.ExtractValue(source), out var result))
		{
			SetValue(BindingNotification.UpdateValue(source, !result), dataValidationError);
			return;
		}
		SetError($"Unable to convert '{source}' to bool.");
	}

	private static bool TryConvert(object? value, out bool result)
	{
		if (value is bool flag)
		{
			result = flag;
			return true;
		}
		if (value is string value2)
		{
			if (bool.TryParse(value2, out result))
			{
				return true;
			}
		}
		else
		{
			try
			{
				result = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				return true;
			}
			catch
			{
			}
		}
		result = false;
		return false;
	}
}
