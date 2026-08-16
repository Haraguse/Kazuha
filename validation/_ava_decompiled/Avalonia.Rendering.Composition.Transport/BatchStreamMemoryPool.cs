using System;
using System.Runtime.InteropServices;

namespace Avalonia.Rendering.Composition.Transport;

internal sealed class BatchStreamMemoryPool : BatchStreamPoolBase<nint>
{
	public int BufferSize { get; }

	public BatchStreamMemoryPool(bool reclaimImmediately, int bufferSize = 1024, Action<Func<bool>>? startTimer = null)
		: base(true, reclaimImmediately, startTimer)
	{
		BufferSize = bufferSize;
	}

	protected override nint CreateItem()
	{
		return Marshal.AllocHGlobal(BufferSize);
	}

	protected override void DestroyItem(nint item)
	{
		Marshal.FreeHGlobal(item);
	}
}
