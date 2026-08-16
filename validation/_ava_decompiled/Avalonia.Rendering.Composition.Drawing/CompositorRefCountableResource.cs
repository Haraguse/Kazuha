using System;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositorRefCountableResource<T> where T : SimpleServerObject
{
	public T Value { get; private set; }

	public int RefCount { get; private set; }

	public CompositorRefCountableResource(T value)
	{
		Value = value;
		RefCount = 1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidOperation()
	{
		throw new InvalidOperationException("This resource is disposed");
	}

	public void AddRef()
	{
		if (RefCount <= 0)
		{
			ThrowInvalidOperation();
		}
		RefCount++;
	}

	public bool Release(Compositor c)
	{
		if (RefCount <= 0)
		{
			ThrowInvalidOperation();
		}
		RefCount--;
		if (RefCount == 0)
		{
			c.DisposeOnNextBatch(Value);
			return true;
		}
		return false;
	}
}
