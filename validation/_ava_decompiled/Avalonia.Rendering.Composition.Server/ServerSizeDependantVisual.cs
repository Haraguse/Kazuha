using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerSizeDependantVisual : ServerCompositionContainerVisual
{
	public ServerSizeDependantVisual(ServerCompositor compositor)
		: base(compositor)
	{
	}

	public override LtrbRect? ComputeOwnContentBounds()
	{
		if (base.Size.X == 0.0 || base.Size.Y == 0.0)
		{
			return null;
		}
		return new LtrbRect(0.0, 0.0, base.Size.X, base.Size.Y);
	}

	protected override void SizeChanged()
	{
		EnqueueForOwnBoundsRecompute();
		base.SizeChanged();
	}
}
