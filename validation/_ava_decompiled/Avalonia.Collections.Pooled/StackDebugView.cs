using System;
using System.Diagnostics;

namespace Avalonia.Collections.Pooled;

internal sealed class StackDebugView<T>
{
	private readonly PooledStack<T> _stack;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _stack.ToArray();

	public StackDebugView(PooledStack<T> stack)
	{
		_stack = stack ?? throw new ArgumentNullException("stack");
	}
}
