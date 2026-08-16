using System;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="T:Avalonia.Data.Core.BindingExpression" /> which uses a function to transform its
/// value.
/// </summary>
internal sealed class FuncTransformNode : ExpressionNode
{
	private readonly Func<object?, object?> _transform;

	public FuncTransformNode(Func<object?, object?> transform)
	{
		_transform = transform;
	}

	public override void BuildString(StringBuilder builder)
	{
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			SetValue(_transform(source));
		}
	}
}
