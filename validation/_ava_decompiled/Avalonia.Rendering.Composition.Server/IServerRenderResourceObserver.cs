namespace Avalonia.Rendering.Composition.Server;

internal interface IServerRenderResourceObserver
{
	void DependencyQueuedInvalidate(IServerRenderResource sender);
}
