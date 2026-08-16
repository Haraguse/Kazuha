using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

/// <summary>
/// A node in an <see cref="T:Avalonia.Data.Core.BindingExpression" /> which casts a value using reflection.
/// </summary>
internal sealed class ReflectionTypeCastNode : ExpressionNode
{
	private readonly Type _targetType;

	public ReflectionTypeCastNode(Type targetType)
	{
		_targetType = targetType;
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('(');
		builder.Append(_targetType.Name);
		builder.Append(')');
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (_targetType.IsInstanceOfType(source))
			{
				SetValue(source);
			}
			else
			{
				ClearValue();
			}
		}
	}
}
