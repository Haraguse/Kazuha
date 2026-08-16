namespace Avalonia.Rendering.Composition.Server;

internal interface IServerRenderResource : IServerRenderResourceObserver
{
	void AddObserver(IServerRenderResourceObserver observer);

	void RemoveObserver(IServerRenderResourceObserver observer);

	void QueuedInvalidate();
}
