namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionVisualCollection : ServerList<ServerCompositionVisual>
{
	internal ServerCompositionVisualCollection(ServerCompositor compositor)
		: base(compositor)
	{
	}
}
