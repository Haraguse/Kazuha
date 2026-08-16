using System;

namespace Avalonia.Rendering.Composition.Transport;

internal sealed class BatchStreamObjectPool<T> : BatchStreamPoolBase<T[]> where T : class?
{
	public int ArraySize { get; }

	public BatchStreamObjectPool(bool reclaimImmediately = false, int arraySize = 128, Action<Func<bool>>? startTimer = null)
		: base(false, reclaimImmediately, startTimer)
	{
		ArraySize = arraySize;
	}

	protected override T[] CreateItem()
	{
		return new T[ArraySize];
	}

	protected override void ClearItem(T[] item)
	{
		Array.Clear(item, 0, item.Length);
	}
}
