namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of <see cref="T:Avalonia.Rendering.Composition.CompositionContainerVisual" />.
/// Mostly propagates update and render calls, but is also responsible
/// for updating adorners in deferred manner
/// </summary>
internal class ServerCompositionContainerVisual : ServerCompositionVisual
{
	public new ServerCompositionVisualCollection Children => base.Children;

	internal ServerCompositionContainerVisual(ServerCompositor compositor)
		: base(compositor)
	{
	}
}
