using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal interface IExpressionForeignFunctionInterface
{
	bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result);
}
