namespace Avalonia.Rendering.Composition.Expressions;

internal interface IExpressionObject
{
	ExpressionVariant GetProperty(string name);
}
