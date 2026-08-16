using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class UnaryExpression : Expression
{
	public Expression Parameter { get; }

	public override ExpressionType Type { get; }

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		if (Type == ExpressionType.Not)
		{
			return !Parameter.Evaluate(ref context);
		}
		if (Type == ExpressionType.UnaryMinus)
		{
			return -Parameter.Evaluate(ref context);
		}
		return default(ExpressionVariant);
	}

	public override void CollectReferences(HashSet<(string parameter, string property)> references)
	{
		Parameter.CollectReferences(references);
	}

	protected override string Print()
	{
		return Expression.OperatorName(Type) + Parameter;
	}

	public UnaryExpression(Expression parameter, ExpressionType type)
	{
		Parameter = parameter;
		Type = type;
	}
}
