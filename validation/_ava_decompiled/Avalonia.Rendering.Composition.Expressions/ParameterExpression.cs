namespace Avalonia.Rendering.Composition.Expressions;

internal class ParameterExpression : Expression
{
	public string Name { get; }

	public override ExpressionType Type => ExpressionType.Parameter;

	public ParameterExpression(string name)
	{
		Name = name;
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		return context.Parameters?.GetParameter(Name) ?? default(ExpressionVariant);
	}

	protected override string Print()
	{
		return "{" + Name + "}";
	}
}
