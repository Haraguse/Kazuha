namespace Avalonia.Rendering.Composition.Expressions;

internal interface IExpressionParameterCollection
{
	ExpressionVariant GetParameter(string name);

	IExpressionObject GetObjectParameter(string name);
}
