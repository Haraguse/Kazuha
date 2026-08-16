namespace Avalonia.Rendering.Composition.Expressions;

internal struct ExpressionEvaluationContext
{
	public ExpressionVariant StartingValue { get; set; }

	public ExpressionVariant CurrentValue { get; set; }

	public ExpressionVariant FinalValue { get; set; }

	public IExpressionObject Target { get; set; }

	public IExpressionParameterCollection Parameters { get; set; }

	public IExpressionForeignFunctionInterface ForeignFunctionInterface { get; set; }
}
