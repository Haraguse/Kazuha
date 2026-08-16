using System;
using Avalonia.Collections.Pooled;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionCacheMode : ServerObject
{
	private readonly WeakHashList<ServerCompositionVisual> _attachedVisuals = new WeakHashList<ServerCompositionVisual>();

	public void Subscribe(ServerCompositionVisual visual)
	{
		_attachedVisuals.Add(visual);
	}

	public void Unsubscribe(ServerCompositionVisual visual)
	{
		_attachedVisuals.Remove(visual);
	}

	protected override void ValuesInvalidated()
	{
		using PooledList<ServerCompositionVisual> pooledList = _attachedVisuals.GetAlive();
		if (pooledList != null)
		{
			Span<ServerCompositionVisual> span = pooledList.Span;
			for (int i = 0; i < span.Length; i++)
			{
				span[i].OnCacheModeStateChanged();
			}
		}
		base.ValuesInvalidated();
	}

	internal ServerCompositionCacheMode(ServerCompositor compositor)
		: base(compositor)
	{
	}
}
