using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class MemberAccessExpression : Expression
{
	public override ExpressionType Type => ExpressionType.MemberAccess;

	public Expression Target { get; }

	public string Member { get; }

	public MemberAccessExpression(Expression target, string member)
	{
		Target = target;
		Member = string.Intern(member);
	}

	public override void CollectReferences(HashSet<(string parameter, string property)> references)
	{
		Target.CollectReferences(references);
		if (Target is ParameterExpression parameterExpression)
		{
			references.Add((parameterExpression.Name, Member));
		}
		else if (Target is KeywordExpression { Keyword: ExpressionKeyword.Target })
		{
			references.Add(("this.target", Member));
		}
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		if (Target is KeywordExpression { Keyword: ExpressionKeyword.Target })
		{
			return context.Target.GetProperty(Member);
		}
		if (Target is ParameterExpression parameterExpression)
		{
			IExpressionObject expressionObject = context.Parameters?.GetObjectParameter(parameterExpression.Name);
			if (expressionObject != null)
			{
				return expressionObject.GetProperty(Member);
			}
		}
		return Target.Evaluate(ref context).GetProperty(Member);
	}

	protected override string Print()
	{
		return "(" + Target.ToString() + ")." + Member;
	}
}
