using System;

namespace Avalonia.Data;

internal class AncestorPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
{
	public Type? AncestorType { get; }

	public int Level { get; }

	public AncestorPathElement(Type? ancestorType, int level)
	{
		AncestorType = ancestorType;
		Level = level;
	}

	public override string ToString()
	{
		return FormattableString.Invariant($"$parent[{AncestorType?.Name},{Level}]");
	}
}
