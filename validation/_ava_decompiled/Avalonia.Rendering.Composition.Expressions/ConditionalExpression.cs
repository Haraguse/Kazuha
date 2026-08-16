using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class ConditionalExpression : Expression
{
	public Expression Condition { get; }

	public Expression TruePart { get; }

	public Expression FalsePart { get; }

	public override ExpressionType Type => ExpressionType.ConditionalExpression;

	public ConditionalExpression(Expression condition, Expression truePart, Expression falsePart)
	{
		Condition = condition;
		TruePart = truePart;
		FalsePart = falsePart;
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		ExpressionVariant expressionVariant = Condition.Evaluate(ref context);
		if (expressionVariant.Type == VariantType.Boolean && expressionVariant.Boolean)
		{
			return TruePart.Evaluate(ref context);
		}
		return FalsePart.Evaluate(ref context);
	}

	public override void CollectReferences(HashSet<(string parameter, string property)> references)
	{
		Condition.CollectReferences(references);
		TruePart.CollectReferences(references);
		FalsePart.CollectReferences(references);
	}

	protected override string Print()
	{
		return $"({Condition}) ? ({TruePart}) : ({FalsePart})";
	}
}
