using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class ServerCompositionRenderData : SimpleServerRenderResource
{
	private RenderDataStream? _stream;

	private PooledInlineList<IServerRenderResource> _referencedResources;

	private LtrbRect? _bounds;

	private bool _boundsValid;

	public LtrbRect? Bounds
	{
		get
		{
			if (!_boundsValid)
			{
				_bounds = CalculateRenderBounds();
				_boundsValid = true;
			}
			return _bounds;
		}
	}

	public ServerCompositionRenderData(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		Reset();
		_stream = new RenderDataStream();
		_stream.DeserializeFrom(reader);
		for (int i = 0; i < _stream.ResourceCount; i++)
		{
			if (_stream.GetResource(i) is IServerRenderResource serverRenderResource)
			{
				_referencedResources.Add(serverRenderResource);
				serverRenderResource.AddObserver(this);
			}
		}
		base.DeserializeChangesCore(reader, committedAt);
	}

	private LtrbRect? CalculateRenderBounds()
	{
		Rect? rect = _stream?.CalculateBounds();
		if (!rect.HasValue)
		{
			return null;
		}
		return ApplyRenderBoundsRounding(new LtrbRect(rect.Value));
	}

	public static Rect? ApplyRenderBoundsRounding(Rect? rect)
	{
		if (!rect.HasValue)
		{
			return null;
		}
		return ApplyRenderBoundsRounding(new LtrbRect(rect.Value))?.ToRect();
	}

	public static LtrbRect? ApplyRenderBoundsRounding(LtrbRect? rect)
	{
		if (rect.HasValue)
		{
			LtrbRect value = rect.Value;
			return new LtrbRect(Math.Floor(value.Left), Math.Floor(value.Top), Math.Ceiling(value.Right), Math.Ceiling(value.Bottom));
		}
		return null;
	}

	public override void DependencyQueuedInvalidate(IServerRenderResource sender)
	{
		_boundsValid = false;
		base.DependencyQueuedInvalidate(sender);
	}

	public void Render(IDrawingContextImpl context)
	{
		_stream?.Replay(context);
	}

	private void Reset()
	{
		_bounds = null;
		_boundsValid = false;
		foreach (IServerRenderResource referencedResource in _referencedResources)
		{
			referencedResource.RemoveObserver(this);
		}
		_referencedResources.Dispose();
		if (_stream != null)
		{
			_stream.DisposeResources();
			_stream.Dispose();
			_stream = null;
		}
	}

	public override void Dispose()
	{
		Reset();
		base.Dispose();
	}
}
