using System.Globalization;

namespace Avalonia.Rendering.Composition.Expressions;

internal class ConstantExpression : Expression
{
	public float Constant { get; }

	public override ExpressionType Type => ExpressionType.Constant;

	public ConstantExpression(float constant)
	{
		Constant = constant;
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		return Constant;
	}

	protected override string Print()
	{
		return Constant.ToString(CultureInfo.InvariantCulture);
	}
}
