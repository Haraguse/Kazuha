using System;

namespace Avalonia.Data;

internal class TypeCastPathElement<T> : ITypeCastElement, ICompiledBindingPathElement
{
	public Type Type => typeof(T);

	public Func<object?, object?> Cast { get; } = TryCast;

	private static object? TryCast(object? obj)
	{
		if (obj is T val)
		{
			return val;
		}
		return null;
	}

	public override string ToString()
	{
		return "(" + Type.FullName + ")";
	}
}
internal class TypeCastPathElement : ITypeCastElement, ICompiledBindingPathElement
{
	public Type Type { get; }

	public Func<object?, object?> Cast { get; }

	public TypeCastPathElement(Type type)
	{
		Type = type;
		Cast = delegate(object? obj)
		{
			if (obj != null)
			{
				if (type.IsInstanceOfType(obj))
				{
					return obj;
				}
			}
			return (object?)null;
		};
	}

	public override string ToString()
	{
		return "(" + Type.FullName + ")";
	}
}
