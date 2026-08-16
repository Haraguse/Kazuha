using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Data;

internal class ArrayElementPathElement : ICompiledBindingPathElement
{
	public int[] Indices { get; }

	public Type ElementType { get; }

	public ArrayElementPathElement(int[] indices, Type elementType)
	{
		Indices = indices;
		ElementType = elementType;
	}

	public override string ToString()
	{
		return FormattableString.Invariant(FormattableStringFactory.Create("[{0}]", string.Join(",", Indices)));
	}
}
