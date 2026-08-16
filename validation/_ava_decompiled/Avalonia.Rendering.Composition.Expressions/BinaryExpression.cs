using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class BinaryExpression : Expression
{
	public Expression Left { get; }

	public Expression Right { get; }

	public override ExpressionType Type { get; }

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		ExpressionVariant expressionVariant = Left.Evaluate(ref context);
		ExpressionVariant expressionVariant2 = Right.Evaluate(ref context);
		if (Type == ExpressionType.Add)
		{
			return expressionVariant + expressionVariant2;
		}
		if (Type == ExpressionType.Subtract)
		{
			return expressionVariant - expressionVariant2;
		}
		if (Type == ExpressionType.Multiply)
		{
			return expressionVariant * expressionVariant2;
		}
		if (Type == ExpressionType.Divide)
		{
			return expressionVariant / expressionVariant2;
		}
		if (Type == ExpressionType.Remainder)
		{
			return expressionVariant % expressionVariant2;
		}
		if (Type == ExpressionType.MoreThan)
		{
			return expressionVariant > expressionVariant2;
		}
		if (Type == ExpressionType.LessThan)
		{
			return expressionVariant < expressionVariant2;
		}
		if (Type == ExpressionType.MoreThanOrEqual)
		{
			return expressionVariant > expressionVariant2;
		}
		if (Type == ExpressionType.LessThanOrEqual)
		{
			return expressionVariant < expressionVariant2;
		}
		if (Type == ExpressionType.LogicalAnd)
		{
			return expressionVariant.And(expressionVariant2);
		}
		if (Type == ExpressionType.LogicalOr)
		{
			return expressionVariant.Or(expressionVariant2);
		}
		if (Type == ExpressionType.Equals)
		{
			return expressionVariant.EqualsTo(expressionVariant2);
		}
		if (Type == ExpressionType.NotEquals)
		{
			return expressionVariant.NotEqualsTo(expressionVariant2);
		}
		return default(ExpressionVariant);
	}

	public override void CollectReferences(HashSet<(string parameter, string property)> references)
	{
		Left.CollectReferences(references);
		Right.CollectReferences(references);
	}

	protected override string Print()
	{
		return "(" + Left?.ToString() + Expression.OperatorName(Type) + Right?.ToString() + ")";
	}

	public BinaryExpression(Expression left, Expression right, ExpressionType type)
	{
		Left = left;
		Right = right;
		Type = type;
	}
}
