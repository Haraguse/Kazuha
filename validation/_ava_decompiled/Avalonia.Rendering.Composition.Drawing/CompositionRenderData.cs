using System;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionRenderData : ICompositorSerializable, IDisposable
{
	private readonly Compositor _compositor;

	private readonly RenderDataStream _stream;

	private PooledInlineList<ICompositionRenderResource> _resources;

	private bool _itemsSent;

	public ServerCompositionRenderData Server { get; }

	public CompositionRenderData(Compositor compositor, RenderDataStream stream)
	{
		_compositor = compositor;
		_stream = stream;
		Server = new ServerCompositionRenderData(compositor.Server);
	}

	public void AddResource(ICompositionRenderResource resource)
	{
		_resources.Add(resource);
	}

	public void Dispose()
	{
		if (!_itemsSent)
		{
			_stream.DisposeResources();
		}
		foreach (ICompositionRenderResource resource in _resources)
		{
			resource.ReleaseOnCompositor(_compositor);
		}
		_resources.Dispose();
		_stream.Dispose();
		_itemsSent = false;
		_compositor.DisposeOnNextBatch(Server);
	}

	public SimpleServerObject TryGetServer(Compositor c)
	{
		return Server;
	}

	public void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		_stream.SerializeTo(writer);
		_itemsSent = true;
	}

	public bool HitTest(Point pt)
	{
		return _stream.HitTest(pt);
	}
}
