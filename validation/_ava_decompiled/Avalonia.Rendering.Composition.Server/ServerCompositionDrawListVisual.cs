using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of <see cref="T:Avalonia.Rendering.Composition.CompositionDrawListVisual" />
/// </summary>
internal class ServerCompositionDrawListVisual : ServerCompositionContainerVisual, IServerRenderResourceObserver
{
	private ServerCompositionRenderData? _renderCommands;

	public ServerCompositionDrawListVisual(ServerCompositor compositor, Visual v)
		: base(compositor)
	{
	}

	public override LtrbRect? ComputeOwnContentBounds()
	{
		return _renderCommands?.Bounds;
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		if (reader.Read<byte>() == 1)
		{
			_renderCommands?.Dispose();
			_renderCommands = reader.ReadObject<ServerCompositionRenderData>();
			_renderCommands?.AddObserver(this);
			InvalidateContent();
		}
		base.DeserializeChangesCore(reader, committedAt);
	}

	protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
	{
		_renderCommands?.Render(context.Canvas);
	}

	public void DependencyQueuedInvalidate(IServerRenderResource sender)
	{
		InvalidateContent();
	}
}
