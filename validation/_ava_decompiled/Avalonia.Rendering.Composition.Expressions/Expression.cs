using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Avalonia.Rendering.Composition.Expressions;

/// <summary>
/// A parsed composition expression
/// </summary>
internal abstract class Expression
{
	public abstract ExpressionType Type { get; }

	public static Expression Parse(string expression)
	{
		return ExpressionParser.Parse(expression.AsSpan());
	}

	public abstract ExpressionVariant Evaluate(ref ExpressionEvaluationContext context);

	public virtual void CollectReferences(HashSet<(string parameter, string property)> references)
	{
	}

	protected abstract string Print();

	public override string ToString()
	{
		return Print();
	}

	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "This method is design time only.")]
	internal static string OperatorName(ExpressionType t)
	{
		PrettyPrintStringAttribute customAttribute = typeof(ExpressionType).GetMember(t.ToString())[0].GetCustomAttribute<PrettyPrintStringAttribute>();
		if (customAttribute != null)
		{
			return customAttribute.Name;
		}
		return t.ToString();
	}
}
