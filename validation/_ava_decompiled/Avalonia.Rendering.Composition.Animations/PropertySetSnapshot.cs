using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// A snapshot of properties used by an animation
/// </summary>
internal class PropertySetSnapshot : IExpressionParameterCollection, IExpressionObject
{
	public struct Value(IExpressionObject o)
	{
		public ExpressionVariant Variant = default(ExpressionVariant);

		public IExpressionObject Object = o;

		public static implicit operator Value(ExpressionVariant v)
		{
			return new Value
			{
				Variant = v
			};
		}
	}

	private readonly Dictionary<string, Value> _dic;

	public PropertySetSnapshot(Dictionary<string, Value> dic)
	{
		_dic = dic;
	}

	public ExpressionVariant GetParameter(string name)
	{
		_dic.TryGetValue(name, out var value);
		return value.Variant;
	}

	public IExpressionObject GetObjectParameter(string name)
	{
		_dic.TryGetValue(name, out var value);
		return value.Object;
	}

	public ExpressionVariant GetProperty(string name)
	{
		return GetParameter(name);
	}
}
