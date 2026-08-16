namespace Avalonia.Rendering.Composition.Expressions;

internal class KeywordExpression : Expression
{
	public override ExpressionType Type => ExpressionType.Keyword;

	public ExpressionKeyword Keyword { get; }

	public KeywordExpression(ExpressionKeyword keyword)
	{
		Keyword = keyword;
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		if (Keyword == ExpressionKeyword.StartingValue)
		{
			return context.StartingValue;
		}
		if (Keyword == ExpressionKeyword.CurrentValue)
		{
			return context.CurrentValue;
		}
		if (Keyword == ExpressionKeyword.FinalValue)
		{
			return context.FinalValue;
		}
		if (Keyword == ExpressionKeyword.Target)
		{
			return default(ExpressionVariant);
		}
		if (Keyword == ExpressionKeyword.True)
		{
			return true;
		}
		if (Keyword == ExpressionKeyword.False)
		{
			return false;
		}
		if (Keyword == ExpressionKeyword.Pi)
		{
			return 3.1415927410125732;
		}
		return default(ExpressionVariant);
	}

	protected override string Print()
	{
		return "[" + Keyword.ToString() + "]";
	}
}
