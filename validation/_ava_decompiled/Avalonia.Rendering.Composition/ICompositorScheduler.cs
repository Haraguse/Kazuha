namespace Avalonia.Rendering.Composition;

internal interface ICompositorScheduler
{
	void CommitRequested(Compositor compositor);
}
