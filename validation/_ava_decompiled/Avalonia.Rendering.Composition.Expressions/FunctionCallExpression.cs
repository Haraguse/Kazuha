using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class FunctionCallExpression : Expression
{
	public string Name { get; }

	public List<Expression> Parameters { get; }

	public override ExpressionType Type => ExpressionType.FunctionCall;

	public FunctionCallExpression(string name, List<Expression> parameters)
	{
		Name = name;
		Parameters = parameters;
	}

	public override ExpressionVariant Evaluate(ref ExpressionEvaluationContext context)
	{
		List<ExpressionVariant> list = new List<ExpressionVariant>();
		foreach (Expression parameter in Parameters)
		{
			list.Add(parameter.Evaluate(ref context));
		}
		if (!context.ForeignFunctionInterface.Call(Name, list, out var result))
		{
			return default(ExpressionVariant);
		}
		return result;
	}

	public override void CollectReferences(HashSet<(string parameter, string property)> references)
	{
		foreach (Expression parameter in Parameters)
		{
			parameter.CollectReferences(references);
		}
	}

	protected override string Print()
	{
		return Name + "( (" + string.Join("), (", Parameters) + ") )";
	}
}
