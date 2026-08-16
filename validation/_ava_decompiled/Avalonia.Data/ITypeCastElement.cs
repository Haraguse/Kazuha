using System;

namespace Avalonia.Data;

internal interface ITypeCastElement : ICompiledBindingPathElement
{
	Type Type { get; }

	Func<object?, object?> Cast { get; }
}
